using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace xmlParsing
{
    public partial class Form1 : Form
    {
        string xmlFilePath = @"C:\Users\kagan\Desktop\deneme.xml";
        Dictionary<string, MainFieldInfo> metadata;

        // Tag classes for TreeView nodes
        public class MainFieldNodeTag
        {
            public string FieldName { get; set; }
            public BigInteger? Value { get; set; }
        }
        public class SubfieldNodeTag
        {
            public string SubfieldName { get; set; }
            public BigInteger? Value { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
        }
        public class SubfieldInfo
        {
            public string Name { get; set; }
            public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        }
        public class MainFieldInfo
        {
            public string Name { get; set; }
            public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
            public List<SubfieldInfo> Subfields { get; set; } = new List<SubfieldInfo>();
        }
        public static Dictionary<string, MainFieldInfo> GetFieldMetadata(string xmlFilePath)
        {
            XDocument doc = XDocument.Load(xmlFilePath);
            var metadata = new Dictionary<string, MainFieldInfo>();

            foreach (var mainField in doc.Root.Elements("cbit"))
            {
                string fieldName = mainField.Attribute("name")?.Value ?? throw new ArgumentException("Main field missing name");
                var mainFieldAttributes = mainField.Attributes()
                    .ToDictionary(attr => attr.Name.LocalName, attr => attr.Value);

                var mainFieldInfo = new MainFieldInfo
                {
                    Name = fieldName,
                    Attributes = mainFieldAttributes
                };

                foreach (var subfield in mainField.Elements("sub"))
                {
                    string subfieldName = subfield.Attribute("name")?.Value ?? throw new ArgumentException("Subfield missing name");
                    var subfieldAttributes = subfield.Attributes()
                        .ToDictionary(attr => attr.Name.LocalName, attr => attr.Value);

                    mainFieldInfo.Subfields.Add(new SubfieldInfo
                    {
                        Name = subfieldName,
                        Attributes = subfieldAttributes
                    });
                }

                metadata[fieldName] = mainFieldInfo;
            }

            return metadata;
        }

        // Same as previous: SubfieldResult, MainFieldResult, ParseStatusMessage
        public class SubfieldResult
        {
            public BigInteger Value { get; set; }
            public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        }

        public class MainFieldResult
        {
            public BigInteger Value { get; set; }
            public Dictionary<string, SubfieldResult> Subfields { get; set; } = new Dictionary<string, SubfieldResult>();
        }

        public static Dictionary<string, MainFieldResult> ParseStatusMessage(string xmlFilePath, byte[] statusMessage)
        {
            var metadata = GetFieldMetadata(xmlFilePath);
            var result = new Dictionary<string, MainFieldResult>();

            foreach (var mainField in metadata.Values)
            {
                string fieldName = mainField.Name;
                if (!mainField.Attributes.TryGetValue("offset", out string offsetStr))
                    throw new ArgumentException($"Main field {fieldName} missing offset");
                int offset = int.Parse(offsetStr);

                int fieldLength = mainField.Attributes.TryGetValue("length", out string lengthStr)
                    ? int.Parse(lengthStr)
                    : mainField.Subfields
                        .Select(sf => (BigInteger.Parse(sf.Attributes.TryGetValue("range_mask", out string mask) ? mask : "0", System.Globalization.NumberStyles.HexNumber).ToByteArray().Length))
                        .DefaultIfEmpty(1)
                        .Max();

                if (offset + fieldLength > statusMessage.Length)
                    throw new ArgumentException($"Offset {offset} with length {fieldLength} exceeds status message length {statusMessage.Length}");

                byte[] fieldBytes = statusMessage.Skip(offset).Take(fieldLength).ToArray();
                Array.Reverse(fieldBytes);
                BigInteger fieldValue = new BigInteger(fieldBytes.Concat(new byte[] { 0 }).ToArray());

                var mainFieldResult = new MainFieldResult { Value = fieldValue };
                var subfields = new Dictionary<string, SubfieldResult>();

                foreach (var subfield in mainField.Subfields)
                {
                    if (!subfield.Attributes.TryGetValue("range_mask", out string rangeMaskStr))
                        throw new ArgumentException($"Subfield {subfield.Name} missing range_mask");
                    BigInteger rangeMask = BigInteger.Parse(rangeMaskStr, System.Globalization.NumberStyles.HexNumber);

                    BigInteger value = fieldValue & rangeMask;
                    int shift = 0;
                    if (rangeMask != 0)
                    {
                        BigInteger temp = rangeMask & -rangeMask;
                        while (temp > 1)
                        {
                            temp >>= 1;
                            shift++;
                        }
                    }
                    if (shift > 0)
                        value >>= shift;

                    if (subfield.Attributes.TryGetValue("mbps_10", out string mbps10) && mbps10 == "true")
                        value *= 10;

                    subfields[subfield.Name] = new SubfieldResult
                    {
                        Value = value,
                        Attributes = subfield.Attributes
                    };
                }

                mainFieldResult.Subfields = subfields;
                result[fieldName] = mainFieldResult;
            }

            return result;
        }
        private void LoadTreeView()
        {
            try
            {
                // Load metadata
                metadata = GetFieldMetadata(xmlFilePath);
                treeViewStatus.Nodes.Clear();

                // Populate TreeView with main fields and subfields
                foreach (var mainField in metadata)
                {
                    // Main field node
                    string offset = mainField.Value.Attributes.TryGetValue("offset", out string offsetStr) ? offsetStr : "N/A";
                    string length = mainField.Value.Attributes.TryGetValue("length", out string lengthStr) ? lengthStr : "N/A";
                    string mainFieldText = $"{mainField.Key} (offset={offset}, length={length}, Value: N/A)";
                    var mainNode = new TreeNode(mainFieldText)
                    {
                        Tag = new MainFieldNodeTag { FieldName = mainField.Key, Value = null }
                    };

                    // Add main field attributes
                    foreach (var attr in mainField.Value.Attributes)
                    {
                        mainNode.Nodes.Add(new TreeNode($"{attr.Key}: {attr.Value}"));
                    }

                    // Add subfields
                    foreach (var subfield in mainField.Value.Subfields)
                    {
                        string rangeMask = subfield.Attributes.TryGetValue("range_mask", out string mask) ? mask : "N/A";
                        string subfieldText = $"{subfield.Name} (range_mask={rangeMask}, Value: N/A)";
                        var subNode = new TreeNode(subfieldText)
                        {
                            Tag = new SubfieldNodeTag
                            {
                                SubfieldName = subfield.Name,
                                Value = null,
                                Attributes = subfield.Attributes
                            }
                        };

                        // Add subfield attributes
                        foreach (var attr in subfield.Attributes)
                        {
                            subNode.Nodes.Add(new TreeNode($"{attr.Key}: {attr.Value}"));
                        }

                        mainNode.Nodes.Add(subNode);
                    }

                    treeViewStatus.Nodes.Add(mainNode);
                }

                treeViewStatus.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading metadata: {ex.Message}");
            }
        }
        private void UpdateTreeViewWithStatusMessage(byte[] statusMessage)
        {
            try
            {
                // Parse status message
                var parsedData = ParseStatusMessage(xmlFilePath, statusMessage);

                // Update TreeView nodes
                foreach (TreeNode mainNode in treeViewStatus.Nodes)
                {
                    var mainTag = mainNode.Tag as MainFieldNodeTag;
                    if (mainTag == null) continue;
                    string fieldName = mainTag.FieldName;

                    if (parsedData.TryGetValue(fieldName, out var mainFieldResult))
                    {
                        // Update main field node
                        string offset = metadata[fieldName].Attributes.TryGetValue("offset", out string offsetStr) ? offsetStr : "N/A";
                        string length = metadata[fieldName].Attributes.TryGetValue("length", out string lengthStr) ? lengthStr : "N/A";
                        mainNode.Text = $"{fieldName} (offset={offset}, length={length}, Value: 0x{mainFieldResult.Value:X})";
                        mainNode.Tag = new MainFieldNodeTag { FieldName = fieldName, Value = mainFieldResult.Value };
                        mainNode.BackColor = Color.White; // Reset background

                        // Update subfield nodes
                        foreach (TreeNode subNode in mainNode.Nodes)
                        {
                            var subTag = subNode.Tag as SubfieldNodeTag;
                            if (subTag == null)
                            {
                                subNode.BackColor = Color.White; // Reset attribute node background
                                continue; // Skip attribute nodes
                            }
                            string subfieldName = subTag.SubfieldName;

                            if (mainFieldResult.Subfields.TryGetValue(subfieldName, out var subfieldResult))
                            {
                                // Update subfield text with value
                                string rangeMask = subTag.Attributes.TryGetValue("range_mask", out string mask) ? mask : "N/A";
                                subNode.Text = $"{subfieldName} (range_mask={rangeMask}, Value: {subfieldResult.Value})";
                                subNode.Tag = new SubfieldNodeTag
                                {
                                    SubfieldName = subfieldName,
                                    Value = subfieldResult.Value,
                                    Attributes = subTag.Attributes
                                };

                                // Colorize background based on conditions
                                bool isError = false;
                                if (subTag.Attributes.TryGetValue("fault", out string fault) && fault != "none")
                                    isError = subfieldResult.Value > 0; // Non-zero fault
                                else if (subTag.Attributes.TryGetValue("error_code", out string errorCode))
                                    isError = subfieldResult.Value > 0; // Non-zero error code
                                else
                                    isError = subfieldResult.Value > 0; // Default: non-zero is error

                                subNode.BackColor = isError ? Color.Red : Color.Green;
                                subNode.ForeColor = Color.White;
                                subNode.Collapse();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating TreeView: {ex.Message}");
            }
        }
        public Form1()
        {
            InitializeComponent();
            try
            {
                // Get metadata
                metadata = GetFieldMetadata(xmlFilePath);

                // Load metadata and populate TreeView
                LoadTreeView();

                // Parse status message
                byte[] statusMessage = new byte[28] { 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x00, 0x11 };

                UpdateTreeViewWithStatusMessage(statusMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
