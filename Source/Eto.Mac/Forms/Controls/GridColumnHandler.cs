using System;
using Eto.Forms;
using Eto.Drawing;
using Eto.Mac.Drawing;
using Eto.Mac.Forms.Cells;


#if XAMMAC2
using AppKit;
using Foundation;
using CoreGraphics;
using ObjCRuntime;
using CoreAnimation;
#else
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.CoreGraphics;
using MonoMac.ObjCRuntime;
using MonoMac.CoreAnimation;
#if Mac64
using CGSize = MonoMac.Foundation.NSSize;
using CGRect = MonoMac.Foundation.NSRect;
using CGPoint = MonoMac.Foundation.NSPoint;
using nfloat = System.Double;
using nint = System.Int64;
using nuint = System.UInt64;
#else
using CGSize = System.Drawing.SizeF;
using CGRect = System.Drawing.RectangleF;
using CGPoint = System.Drawing.PointF;
using nfloat = System.Single;
using nint = System.Int32;
using nuint = System.UInt32;
#endif
#endif

namespace Eto.Mac.Forms.Controls
{
	public interface IDataViewHandler
	{
		bool ShowHeader { get; }

		NSTableView Table { get; }

		object GetItem(int row);

		int RowCount { get; }

		CGRect GetVisibleRect();

		void OnCellFormatting(GridColumn column, object item, int row, NSCell cell);
	}

	public interface IDataColumnHandler
	{
		void Setup(int column);

		NSObject GetObjectValue(object dataItem);

		void SetObjectValue(object dataItem, NSObject val);

		GridColumn Widget { get; }

		IDataViewHandler DataViewHandler { get; }

		void Resize(bool force = false);

		void Loaded(IDataViewHandler handler, int column);
	}

	public class GridColumnHandler : MacObject<NSTableColumn, GridColumn, GridColumn.ICallback>, GridColumn.IHandler, IDataColumnHandler
	{
		Cell dataCell;
		Font font;

		public IDataViewHandler DataViewHandler { get; private set; }

		public int Column { get; private set; }

		public GridColumnHandler()
		{
			Control = new NSTableColumn();
			Control.ResizingMask = NSTableColumnResizing.None;
			Sortable = false;
			HeaderText = string.Empty;
			Editable = false;
			AutoSize = true;
			DataCell = new TextBoxCell();
		}

		public void Loaded(IDataViewHandler handler, int column)
		{
			Column = column;
			DataViewHandler = handler;
		}

		public void Resize(bool force = false)
		{
			var handler = DataViewHandler;
			if (AutoSize && handler != null)
			{
				var width = Control.DataCell.CellSize.Width;
				var outlineView = handler.Table as NSOutlineView;
				if (handler.ShowHeader)
					width = (nfloat)Math.Max(Control.HeaderCell.CellSize.Width, width);
					
				if (dataCell != null)
				{
					/* Auto size based on visible cells only */
					var rect = handler.GetVisibleRect();
					var range = handler.Table.RowsInRect(rect);

					var cellSize = Control.DataCell.CellSize;
					var dataCellHandler = ((ICellHandler)dataCell.Handler);
					for (var i = range.Location; i < range.Location + range.Length; i++)
					{
						var cellWidth = GetRowWidth(dataCellHandler, (int)i, cellSize) + 4;
						if (outlineView != null && Column == 0)
						{
							cellWidth += (float)((outlineView.LevelForRow((nint)i) + 1) * outlineView.IndentationPerLevel);
						}
						width = (nfloat)Math.Max(width, cellWidth);
					}
				}
				if (force || width > Control.Width)
					Control.Width = width;
			}
		}

		protected virtual nfloat GetRowWidth(ICellHandler cell, int row, CGSize cellSize)
		{
			var item = DataViewHandler.GetItem(row);
			var val = GetObjectValue(item);
			return cell.GetPreferredSize(val, cellSize, row, item);
		}

		public string HeaderText
		{
			get { return Control.HeaderCell.StringValue; }
			set { Control.HeaderCell.StringValue = value; }
		}

		public bool Resizable
		{
			get
			{
				return Control.ResizingMask.HasFlag(NSTableColumnResizing.UserResizingMask);
			}
			set
			{
				if (value)
					Control.ResizingMask |= NSTableColumnResizing.UserResizingMask;
				else
					Control.ResizingMask &= ~NSTableColumnResizing.UserResizingMask;
			}
		}

		public bool AutoSize
		{
			get;
			set;
		}

		public bool Sortable { get; set; }

		public bool Editable
		{
			get
			{
				return Control.Editable;
			}
			set
			{
				Control.Editable = value;
				if (dataCell != null)
				{
					var cellHandler = (ICellHandler)dataCell.Handler;
					cellHandler.Editable = value;
				}
			}
		}

		public int Width
		{
			get { return (int)Control.Width; }
			set { Control.Width = value; }
		}

		public bool Visible
		{
			get { return !Control.Hidden; }
			set { Control.Hidden = !value; }
		}

		public Cell DataCell
		{
			get { return dataCell; }
			set
			{
				dataCell = value;
				if (dataCell != null)
				{
					var editable = Editable;
					var cellHandler = (ICellHandler)dataCell.Handler;
					Control.DataCell = cellHandler.Control;
					cellHandler.ColumnHandler = this;
					cellHandler.Editable = editable;
				}
				else
					Control.DataCell = null;
			}
		}

		public void Setup(int column)
		{
			Column = column;
			Control.Identifier = new NSString(column.ToString());
		}

		public NSObject GetObjectValue(object dataItem)
		{
			return ((ICellHandler)dataCell.Handler).GetObjectValue(dataItem);
		}

		public void SetObjectValue(object dataItem, NSObject val)
		{
			((ICellHandler)dataCell.Handler).SetObjectValue(dataItem, val);
		}

		GridColumn IDataColumnHandler.Widget
		{
			get { return Widget; }
		}

		public Font Font
		{
			get
			{
				if (font == null)
					font = new Font(new FontHandler(Control.DataCell.Font));
				return font;
			}
			set
			{
				font = value;
				if (font != null)
				{
					var fontHandler = (FontHandler)font.Handler;
					Control.DataCell.Font = fontHandler.Control;
				}
				else
					Control.DataCell.Font = null;
			}
		}
	}
}

