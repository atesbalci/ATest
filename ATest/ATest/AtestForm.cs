using System;
using System.Collections.ObjectModel;
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
	    private readonly FileFilter _filter;

        private string _openFile;
	    private Node _lastShownNode;
	    private bool _isChanged;

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
			ClientSize = new Size(900, 700);
		    var modifier = Platform.Detect.IsMac ? Keys.Alt : Keys.Control;

            var layout = new DynamicLayout
            {
                Padding = 10,
                Spacing = new Size(10, 10)
            };

		    Content = layout;

		    _rootNode = new Category { Name = "Root" };
		    _lastSaveLocationFile = Path.Combine(EtoEnvironment.GetFolderPath(EtoSpecialFolder.ApplicationSettings),
		        "LastSaveLocation.txt");
            
		    layout.Add(new Label
		    {
		        Text = modifier + "+UP/DOWN to move the selected node.\n" +
		               modifier + "+Click to change the parent of the selected node.\n" +
		               modifier + "+D to remove node."
            });

            #region Save/Load Stuff

		    _isChanged = true;
            try
            {
                var file = File.ReadAllText(_lastSaveLocationFile);
                _rootNode.FromXml(XDocument.Parse(File.ReadAllText(file)).Root);
                OpenFile = file;
                _isChanged = false;
            }
		    catch (Exception)
		    {
		        // ignored
		    }

		    _filter = new FileFilter
		    {
		        Extensions = new[] { "xml", "XML" },
		        Name = "XML"
		    };
		    var loadCommand = new Command { ToolBarText = "Load" };
		    loadCommand.Executed += (sender, e) =>
		    {
		        var dialog = new OpenFileDialog
		        {
		            Filters = { _filter },
		            CurrentFilter = _filter
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
		    saveAsCommand.Executed += (sender, e) => SaveAs();
		    var saveCommand = new Command { ToolBarText = "Save" };
		    saveCommand.Executed += (sender, e) => Save();
		    ToolBar = new ToolBar { Items = { loadCommand, saveCommand, saveAsCommand } };

		    Closing += (sender, args) =>
		    {
		        if (!_isChanged) return;
		        args.Cancel = true;
                var dialog = new Dialog
                {
                    Title = "Save Changes"
                };
		        var abortButton = new Button((o, eventArgs) => dialog.Close())
		        {
		            Text = "Cancel"
		        };
		        var saveButton = new Button((o, eventArgs) =>
		        {
                    if(Save())
                    {
		                args.Cancel = false;
                        dialog.Close();
                    }
                })
		        {
                    Text = "Save"
		        };
                var dontSaveButton = new Button((o, eventArgs) =>
                {
                    args.Cancel = false;
                    dialog.Close();
                })
                {
                    Text = "Discard"
                };
                dialog.NegativeButtons.Add(abortButton);
                dialog.PositiveButtons.Add(dontSaveButton);
                dialog.PositiveButtons.Add(saveButton);
		        dialog.AbortButton = abortButton;
                dialog.ShowModal(this);
		    };

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
		    _tree.CellClick += (sender, args) =>
		    {
                if (args.Modifiers == modifier && args.Buttons == MouseButtons.Primary)
		        {
		            var selected = GetNodeFromTreeItem(_tree.SelectedItem);
		            if (selected == null || selected == _rootNode) return;
		            var newParent = GetNodeFromTreeItem(args.Item);
		            if (newParent is TestCase || IsChildOf((ITreeGridItem) args.Item, _tree.SelectedItem)) return;
		            var parent = GetNodeFromTreeItem(_tree.SelectedItem.Parent);
		            parent.Children.Remove(selected);
                    newParent.Children.Add(selected);
                    args.Handled = true;
                    RefreshTree();
		        }
            };
		    _tree.KeyDown += (sender, args) =>
		    {
                if (modifier == Keys.Alt ? args.Alt : args.Control)
		        {
		            switch (args.Key)
		            {
		                case Keys.Up:
                            MoveNode(-1);
		                    args.Handled = true;
		                    break;
                        case Keys.Down:
                            MoveNode(1);
		                    args.Handled = true;
                            break;
                        case Keys.D:
                            RemoveNode();
                            args.Handled = true;
                            break;
		            }
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
	        //var testCase = node as TestCase;
	        //if (testCase != null)
	        //{
	        //    return testCase.Performed;
	        //}
	        //return node.Children.All(IsNodePerformed);
	        return node.Performed;
	    }

	    public static Node FindParentOfNode(Node cur, Node node)
	    {
	        if (cur.Children.Contains(node))
	        {
	            return cur;
	        }
	        foreach (var child in cur.Children)
	        {
	            var result = FindParentOfNode(child, node);
	            if (result != null)
	            {
	                return result;
	            }
	        }
	        return null;
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

	    public void MoveNode(int amount)
	    {
	        var node = GetNodeFromTreeItem(_tree.SelectedItem);
	        if (node == null || node == _rootNode) return;
	        var parent = GetNodeFromTreeItem(_tree.SelectedItem.Parent);
            var index = parent.Children.IndexOf(node);
            parent.Children.RemoveAt(index);
	        var newIndex = Math.Max(0, Math.Min(parent.Children.Count, index + amount));
            parent.Children.Insert(newIndex, node);
            RefreshTree();
	    }

	    public void RemoveNode()
	    {
	        var node = GetNodeFromTreeItem(_tree.SelectedItem);
	        if (node == null || node == _rootNode) return;
            var parent = GetNodeFromTreeItem(_tree.SelectedItem.Parent);
	        parent.Children.Remove(node);
            RefreshTree();
	    }

        public void RefreshTree()
	    {
            var selected = GetNodeFromTreeItem(_tree.SelectedItem);
            var col = new TreeGridItemCollection { Populate(_rootNode) };
            _tree.DataStore = col;
            _tree.ReloadData();
            if (selected != null)
            {
                _tree.SelectedItem = FindNode((TreeGridItem)col[0], selected);
            }
	        _isChanged = true;
	    }

        public static TreeGridItem FindNode(TreeGridItem item, Node node)
        {
            if (GetNodeFromTreeItem(item) == node) return item;
            foreach(var child in item.Children)
            {
                var result = FindNode((TreeGridItem)child, node);
                if(result != null)
                {
                    return result;
                }
            }
            return null;
        }

	    public NodeTreeGridItem Populate(Node node)
	    {
	        var retVal = new NodeTreeGridItem
            {

                Values = new object[]
                {
                    node.Name,
                    node == _rootNode ? string.Empty : 
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
	        if (_lastShownNode == node || node == _rootNode) return;
	        _lastShownNode = node;
	        _nodeContent.Items.Clear();
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
	                    var textArea = new TextArea();
	                    text = textArea;
                        textArea.TextChanged += (sender, args) =>
                        {
                            var font = textArea.Font;
                            var height = font.MeasureString(textArea.Text + "a\na").Height; //Don't ask me why
                            textArea.Height = Math.Max(textArea.Height, (int)height);
                        };
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
	                if (prop.Name == "Performed")
	                {
	                    checkBox.CheckedChanged += (sender, args) =>
	                    {
	                        var parent = FindParentOfNode(_rootNode, node);
	                        if (parent != null)
	                        {
	                            parent.Performed = parent.Children.All(child => child.Performed);
                                RefreshTree();
	                        }
	                    };
	                }
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

	    public bool Save()
	    {
	        if (!string.IsNullOrEmpty(OpenFile))
	        {
	            File.WriteAllText(OpenFile, _rootNode.ToXml().ToString());
	            return true;
	        }
	        return SaveAs();
	    }

	    public bool SaveAs()
	    {
	        var dialog = new SaveFileDialog
	        {
	            Filters = { _filter },
	            CurrentFilter = _filter
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
	            return true;
	        }
	        return false;
	    }
    }

    public class NodeTreeGridItem : TreeGridItem
    {
        public Node AssignedNode { get; set; }
    }
}
