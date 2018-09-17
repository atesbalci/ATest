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
	public class AtestForm : Form
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

	    public AtestForm()
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
		    _tree = new TreeGridView
		    {
		        RowHeight = 20,
		        AllowMultipleSelection = false,
		        Columns =
		        {
		            new GridColumn
		            {
		                DataCell = new TextBoxCell(0),
		                HeaderText = "Categories",
		                AutoSize = false,
                        Width = 200
		            },
                    new GridColumn
                    {
                        DataCell = new TextBoxCell(1),
                        HeaderText = "Status",
                        AutoSize = false,
                        Width = 100
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
		            }),
                AllowDrop = true
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
		    _tree.CellClick += (sender, args) =>
		    {
		        if ((args.Buttons & MouseButtons.Primary) != 0)
		        {
		            var item = (ITreeGridItem) args.Item;
		            _tree.SelectedItem = item;
		            DoDragDrop(new DataObject(), DragEffects.Move);
		        }
		    };
		    _tree.DragOver += (sender, args) =>
		    {
		        args.Effects = DragEffects.Move;
		    };
		    _tree.DragDrop += (sender, args) =>
		    {
		        var drag = _tree.GetDragInfo(args);
		        if (drag.Item == null
                || drag.Item == _tree.SelectedItem
                || drag.Position != GridDragPosition.Over
                || _tree.SelectedItem.Parent == null
                || GetNodeFromTreeItem(drag.Item) is TestCase
                || IsChildOf((ITreeGridItem) drag.Item, _tree.SelectedItem)) return;
		        GetNodeFromTreeItem(_tree.SelectedItem.Parent).Children.Remove(GetNodeFromTreeItem(_tree.SelectedItem));
		        GetNodeFromTreeItem(drag.Item).Children.Add(GetNodeFromTreeItem(_tree.SelectedItem));
		        RefreshTree();
            };
		    _tree.KeyDown += (sender, args) =>
		    {
		        if (args.Control)
		        {
		            
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

	    public static Node GetNodeFromTreeItem(object item)
	    {
	        return (item as NodeTreeGridItem)?.AssignedNode;
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

	    public static bool IsChildOf(ITreeGridItem item, ITreeGridItem possibleParent)
	    {
	        while (item != null)
	        {
	            if (item.Parent == possibleParent)
	            {
	                return true;
	            }
	            item = item.Parent;
	        }
	        return false;
	    }

        public void RefreshTree()
	    {
	        _tree.DataStore = new TreeGridItemCollection { Populate(_rootNode) };
            _tree.ReloadData();
	    }

	    public NodeTreeGridItem Populate(Node node)
	    {
	        var retVal = new NodeTreeGridItem
            {

                Values = new object[]
                {
                    node.Name,
                    (IsNodePerformed(node) ? "" : "Not ") + "Performed"
                },
	            AssignedNode = node,
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
	        if (node != _rootNode)
	        {
                var layout = new StackLayout { Orientation = Orientation.Horizontal };
	            var upButton = new Button
	            {
	                Text = "Up"
	            };
	            var downButton = new Button
	            {
	                Text = "Down"
	            };
                upButton.Click += (sender, args) =>
	            {
                    var parent = GetNodeFromTreeItem(_tree.SelectedItem.Parent);
	                var index = parent.Children.IndexOf(node);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(Math.Max(index - 1, 0), node);
	            };
                downButton.Click += (sender, args) =>
	            {
	                var parent = GetNodeFromTreeItem(_tree.SelectedItem.Parent);
	                var index = parent.Children.IndexOf(node);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(Math.Min(index + 1, parent.Children.Count - +1), node);
                };
                layout.Items.Add(upButton);
                layout.Items.Add(downButton);
                _nodeContent.Items.Add(layout);
	        }
	        foreach (var prop in node.GetType().GetRuntimeProperties()
                .Where(prop => prop.IsDefined(typeof(NodePropertyAttribute)))
                .OrderBy(prop => ((NodePropertyAttribute)prop.GetCustomAttribute(typeof(NodePropertyAttribute))).Priority))
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

    public class NodeTreeGridItem : TreeGridItem
    {
        public Node AssignedNode { get; set; }
    }
}
