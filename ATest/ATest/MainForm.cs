using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ATest.NodeSystem;
using Eto;
using Eto.Forms;
using Eto.Drawing;

namespace ATest
{
	public class MainForm : Form
	{
	    private readonly string _lastSaveLocationFile;
	    private readonly TreeGridView _tree;
	    private readonly StackLayout _nodeContent;
	    private readonly Node _rootNode;

	    private string _openFile;

	    private string OpenFile
	    {
	        get => _openFile;
	        set
	        {
	            if (_openFile == value) return;
                File.WriteAllText(_lastSaveLocationFile, value);
	            _openFile = value;
	        }
	    }

	    public MainForm()
		{
			Title = "ATest";
			ClientSize = new Size(800, 600);

            var layout = new DynamicLayout()
            {
                Padding = 10
            };

		    Content = layout;

		    _rootNode = new Category { Name = "Root" };
		    _lastSaveLocationFile = Path.Combine(EtoEnvironment.GetFolderPath(EtoSpecialFolder.ApplicationSettings),
		        "LastSaveLocation.txt");

            #region Save/Load Stuff

            try
            {
                OpenFile = File.ReadAllText(_lastSaveLocationFile);
                _rootNode.FromXml(XDocument.Parse(File.ReadAllText(OpenFile)).Root);
		    }
		    catch (Exception)
		    {
		        // ignored
		    }

		    var filter = new FileFilter
		    {
		        Extensions = new[] { "xml", "XML" },
		        Name = "XML"
		    };
		    var loadCommand = new Command { ToolBarText = "Load" };
		    loadCommand.Executed += (sender, e) =>
		    {
		        var dialog = new OpenFileDialog
		        {
		            Filters = { filter },
		            CurrentFilter = filter
		        };
		        var result = dialog.ShowDialog(this);
		        if (result == DialogResult.Yes || result == DialogResult.Ok)
		        {
		            _rootNode.FromXml(XDocument.Parse(File.ReadAllText(dialog.FileName)).Root);
		            RefreshTree();
		            OpenFile = dialog.FileName;
		        }
		    };
		    var saveAsCommand = new Command { ToolBarText = "Save As..." };
		    saveAsCommand.Executed += (sender, e) =>
		    {
		        var dialog = new SaveFileDialog
		        {
		            Filters = { filter },
		            CurrentFilter = filter
		        };
		        var result = dialog.ShowDialog(this);
		        if (result == DialogResult.Yes || result == DialogResult.Ok)
		        {
		            var name = dialog.FileName;
		            if (!name.ToLower().EndsWith(".xml"))
		            {
		                name += ".xml";
		            }
		            OpenFile = name;
		            File.WriteAllText(name, _rootNode.ToXml().ToString());
		        }
		    };
		    var saveCommand = new Command { ToolBarText = "Save" };
		    saveCommand.Executed += (sender, e) =>
		    {
		        if (!string.IsNullOrEmpty(OpenFile))
		        {
		            File.WriteAllText(OpenFile, _rootNode.ToXml().ToString());
		        }
		        else
		        {
		            saveAsCommand.Execute();
		        }
		    };
		    ToolBar = new ToolBar { Items = { loadCommand, saveCommand, saveAsCommand } };

            #endregion

		    #region Program

		    layout.BeginHorizontal();
		    _nodeContent = new StackLayout
		    {
		        Padding = 10,
                Spacing = 5
		    };
		    var addCategoryCommand = new Command();
		    addCategoryCommand.Executed += (sender, args) =>
		    {
		        var node = GetNodeFromTreeItem(_tree.SelectedItem);
		        if (node is Category)
		        {
		            node.Children.Add(new Category { Name = "New Category" });
		            RefreshTree();
		        }
		    };
		    var addTestCaseCommand = new Command();
		    addTestCaseCommand.Executed += (sender, args) =>
		    {
		        var node = GetNodeFromTreeItem(_tree.SelectedItem);
		        if (node is Category)
		        {
		            node.Children.Add(new TestCase { Name = "New Test Case" });
		            RefreshTree();
		        }
		    };
		    var cell = new DrawableCell();
		    cell.Paint += (sender, args) =>
		    {
		        var node = GetNodeFromTreeItem((ITreeGridItem) args.Item);
		        var color = args.IsSelected ? Colors.White : IsNodePerformed(node) ? new Color(0f, 0.5f, 0f) : Colors.Red;
		        args.Graphics.DrawText(new Font(SystemFont.Bold), color, args.ClipRectangle.Location, node.Name);
		    };
		    _tree = new TreeGridView
		    {
		        RowHeight = 20,
		        AllowMultipleSelection = false,
		        Columns =
		        {
		            new GridColumn
		            {
		                DataCell = cell,
		                HeaderText = "Categories",
		                AutoSize = false,
		                Width = 300
		            }
		        },
		        ContextMenu = new ContextMenu(
		            new ButtonMenuItem
		            {
		                Text = "Add Category",
		                Command = addCategoryCommand
		            },
		            new ButtonMenuItem
		            {
		                Text = "Add Test Case",
		                Command = addTestCaseCommand
		            })
		    };
		    _tree.Expanded += (sender, args) => GetNodeFromTreeItem(args.Item).Expanded = true;
		    _tree.Collapsed += (sender, args) => GetNodeFromTreeItem(args.Item).Expanded = false;
		    _tree.SelectionChanged += (sender, args) =>
		    {
		        if (_tree.SelectedItem != null)
		        {
		            ShowNode(GetNodeFromTreeItem(_tree.SelectedItem));
		        }
		    };
		    layout.Add(_tree);
		    layout.Add(new Panel
		    {
		        Content = _nodeContent
		    });
		    RefreshTree();
		    layout.EndHorizontal();

		    #endregion
        }

	    private static Node GetNodeFromTreeItem(ITreeGridItem item)
	    {
	        return (item as TreeGridItem)?.Tag as Node;
	    }

	    public static bool IsNodePerformed(Node node)
	    {
	        var testCase = node as TestCase;
	        if (testCase != null)
	        {
	            return testCase.Performed;
	        }
	        return node.Children.All(IsNodePerformed);
	    }

        public void RefreshTree()
	    {
	        _tree.DataStore = new TreeGridItemCollection { Populate(_rootNode) };
            _tree.ReloadData();
	    }

	    public TreeGridItem Populate(Node node)
	    {
	        var retVal = new TreeGridItem
	        {
	            Tag = node,
                Expanded = node.Expanded
	        };
	        foreach (var nodeChild in node.Children)
	        {
	            retVal.Children.Add(Populate(nodeChild));
	        }
	        return retVal;
	    }

	    public void ShowNode(Node node)
	    {
	        _nodeContent.Items.Clear();
	        foreach (var prop in node.GetType().GetRuntimeProperties()
                .Where(prop => prop.CustomAttributes.Any(attr => attr.AttributeType == typeof(NodePropertyAttribute))))
	        {
	            var val = prop.GetValue(node);
	            var type = prop.PropertyType;
	            var layout = new StackLayout
	            {
	                Orientation = Orientation.Horizontal,
                    Spacing = 5
	            };
	            layout.Items.Add(new Label
	            {
	                Font = new Font(SystemFont.Bold, 12f),
	                Text = prop.Name + ":",
	                Width = 120,
                    TextAlignment = TextAlignment.Right
	            });

                if (val == null) continue;

	            Control control = null;
	            if (type == typeof(string) || type == typeof(int) || type == typeof(float))
	            {
	                var param = ((NodePropertyAttribute) prop.GetCustomAttribute(typeof(NodePropertyAttribute)))
	                    .AdditionalParameter;
	                TextControl text;
	                if (param == NodePropertyAttribute.Parameter.MultiLineString)
	                {
	                    text = new TextArea();
	                }
	                else
	                {
	                    text = new TextBox();
	                }
                    text.Size = new Size(200, text.Size.Height);
	                text.Text = val.ToString();
	                text.TextChanged += (sender, args) =>
	                {
	                    object newVal = null;
	                    if (type == typeof(int))
	                    {
	                        int i;
	                        if (int.TryParse(text.Text, out i))
	                        {
	                            newVal = i;
	                        }
	                    }
                        else if (type == typeof(float))
	                    {
	                        float f;
	                        if (float.TryParse(text.Text, out f))
	                        {
	                            newVal = f;
	                        }
	                    }
	                    else
	                    {
	                        newVal = text.Text;
	                    }

                        if(newVal != null)
                        {
                            prop.SetValue(node, newVal);
                            RefreshTree();
                        }
	                };
	                control = text;
	            }
	            else if (type == typeof(bool))
	            {
	                var checkBox = new CheckBox
	                {
	                    Checked = (bool) val
	                };
	                checkBox.CheckedChanged += (sender, args) =>
	                {
                        prop.SetValue(node, checkBox.Checked);
                        RefreshTree();
	                };
	                control = checkBox;
	            }
                else if (type == typeof(DateTime))
	            {
	                var dateTimePicker = new DateTimePicker
	                {
	                    Value = (DateTime) val
	                };
	                dateTimePicker.ValueChanged += (sender, args) =>
	                {
                        prop.SetValue(node, dateTimePicker.Value);
                        RefreshTree();
	                };
	                control = dateTimePicker;
	            }

	            if (control != null)
	            {
	                control.Width = 300;
	                layout.Items.Add(control);
	            }
                _nodeContent.Items.Add(layout);
	        }
	    }
    }
}
