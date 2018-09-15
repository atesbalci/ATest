using System.IO;
using System.Linq;
using System.Xml.Linq;
using ATest.NodeSystem;
using Eto.Forms;
using Eto.Drawing;

namespace ATest
{
	public class MainForm : Form
	{
	    private readonly TreeGridView _tree;
	    private readonly StackLayout _nodeContent;
	    private readonly Node _rootNode;

	    private string _openFile;

		public MainForm()
		{
			Title = "My Eto Form";
			ClientSize = new Size(800, 600);

            var layout = new DynamicLayout()
            {
                Padding = 10
            };

		    Content = layout;

            // create a few commands that can be used for the menu and toolbar
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
		            _openFile = dialog.FileName;
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
		            _openFile = name;
		            File.WriteAllText(name, _rootNode.ToXml().ToString());
		        }
		    };
		    var saveCommand = new Command { ToolBarText = "Save" };
		    saveCommand.Executed += (sender, e) =>
		    {
		        if (!string.IsNullOrEmpty(_openFile))
		        {
		            File.WriteAllText(_openFile, _rootNode.ToXml().ToString());
		        }
		        else
		        {
		            saveAsCommand.Execute();
		        }
		    };
            ToolBar = new ToolBar { Items = { loadCommand, saveCommand, saveAsCommand } };

            //Program
            _rootNode = new Category { Name = "Root" };
		    layout.BeginHorizontal();
		    _nodeContent = new StackLayout();
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
		        var color = IsNodePerformed(node) ? Colors.Green : Colors.Red;
                args.Graphics.DrawText(new Font(SystemFont.Default), color, args.ClipRectangle.Location, node.Name);
		    };
		    _tree = new TreeGridView
		    {
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
	        var nameBox = new TextBox {Text = node.Name};
	        nameBox.TextChanged += (sender, args) => node.Name = nameBox.Text;
            _nodeContent.Items.Add(nameBox);
	        var testCase = node as TestCase;
	        if (testCase == null) return;
	        var performed = new CheckBox
	        {
	            Text = "Performed",
	            Checked = testCase.Performed
	        };
	        performed.CheckedChanged += (sender, args) =>
	        {
	            testCase.Performed = performed.Checked ?? false;
                RefreshTree();
	        };
            _nodeContent.Items.Add(performed);
	    }
    }
}
