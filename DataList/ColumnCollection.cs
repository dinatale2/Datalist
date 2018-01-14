using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using DebugLib;

namespace DataList
{
	internal delegate void ColumnCollectionChanged(int nIndex);
	internal delegate void ColumnCollectionColumnChanged(Column Col);

	public class ColumnCollection : CollectionBase
	{
		internal event ColumnCollectionChanged ColumnAdded;
		internal event ColumnCollectionChanged ColumnRemoving;
		internal event ColumnCollectionColumnChanged ColumnRemoved;

		private ListWnd m_Parent;
		internal ListWnd Parent
		{
			get { return m_Parent; }
			set { m_Parent = value; }
		}

		public int RowWidth
		{
			get { return GetRowWidth(); }
		}

		public int ColumnHeight
		{
			get { return m_Parent.Font.Height + 2 * m_Parent.CellPadding; }
		}

		internal ColumnCollection(ListWnd parent)
			: base()
		{
			m_Parent = parent;
		}

		public int GetRowWidth()
		{
			int width = 0;

			//lock (List.SyncRoot)
			{
				foreach (Column col in List)
				{
					width += col.Width;
				}
			}

			return width;
		}

		public Column this[int Index]
		{
			get
			{
				return List[Index] as Column;
			}
		}

		public int IndexOf(Column item)
		{
			return List.IndexOf(item);
		}

		internal void HandleSortPriorities(Column PivotCol, int nNewSortPriority)
		{
			int nOldSortPriority = PivotCol.SortPriority;

			if (nNewSortPriority < 0)
				nNewSortPriority = -1;

			if (nNewSortPriority >= 0)
			{
				int nCurrMaxPriority = -1;

				foreach (Column col in List)
				{
					if (col.SortPriority > nCurrMaxPriority)
						nCurrMaxPriority = col.SortPriority;
				}

				if (nNewSortPriority > nCurrMaxPriority)
				{
					if (nOldSortPriority < 0)
						nNewSortPriority = nCurrMaxPriority + 1;
					else
						nNewSortPriority = nCurrMaxPriority;
				}
			}

			if (nOldSortPriority == nNewSortPriority)
				return;

			if (nOldSortPriority < 0)
			{
				foreach (Column col in List)
				{
					if (col.SortPriority >= nNewSortPriority)
					{
						Debug.Assert(col != PivotCol);
						col.SetSortPriority(col.SortPriority + 1, false);
					}
				}
			}
			else if (nNewSortPriority < 0)
			{
				foreach (Column col in List)
				{
					if (col.SortPriority > nOldSortPriority)
					{
						Debug.Assert(col != PivotCol);
						col.SetSortPriority(col.SortPriority - 1, false);
					}
				}
			}
			else if (nNewSortPriority > nOldSortPriority)
			{
				foreach (Column col in List)
				{
					if (col.SortPriority > nOldSortPriority && col.SortPriority <= nNewSortPriority)
					{
						Debug.Assert(col != PivotCol);
						col.SetSortPriority(col.SortPriority - 1, false);
					}
				}
			}
			else if (nNewSortPriority < nOldSortPriority)
			{
				foreach (Column col in List)
				{
					if (col.SortPriority < nOldSortPriority && col.SortPriority >= nNewSortPriority)
					{
						Debug.Assert(col != PivotCol);
						col.SetSortPriority(col.SortPriority + 1, false);
					}
				}
			}

			// TODO: need to decide if I should sort the rows once new row priorities are determined.
			PivotCol.SetSortPriority(nNewSortPriority, false);
		}

		public void Add(Column ToAdd)
		{
			//lock (List.SyncRoot)
			if (!this.Contains(ToAdd))
			{
				ToAdd.Parent = this;
				int nIndex = List.Add(ToAdd);
				ToAdd.Index = nIndex;

				if (ToAdd.SortPriority != -1)
				{
					int nSortPriority = ToAdd.SortPriority;
					ToAdd.SortPriority = -1;
					HandleSortPriorities(ToAdd, nSortPriority);
				}

				if (ColumnAdded != null)
					ColumnAdded(nIndex);
			}
		}

		public void Add(string dispName, DatalistDataTypes valType, int width, ColumnType ColType, bool bAllowEdit)
		{
			Column ToAdd = new Column(dispName, valType, width, ColType, bAllowEdit);
			//lock (List.SyncRoot)
			{
				Add(ToAdd);
			}
		}

		public bool Contains(Column item)
		{
			return List.Contains(item);
		}

		public void Insert(int index, Column item)
		{
			item.Parent = this;
			List.Insert(index, item);
		}

		public void Remove(Column col)
		{
			//lock (List.SyncRoot)
			if (this.Contains(col))
			{
				if (ColumnRemoving != null)
					ColumnRemoving(col.Index);

				col.Parent = null;

				foreach (Column c in List)
				{
					if (c.Index > col.Index)
						c.Index--;
				}

				col.Index = -1;
				int nNewSortPriority = -1;
				HandleSortPriorities(col, nNewSortPriority);
				List.Remove(col);

				if (ColumnRemoved != null)
					ColumnRemoved(col);
			}
		}

		public new void Clear()
		{
			//lock (List.SyncRoot)
			{
				foreach (Column col in List)
				{
					col.Parent = null;
				}

				List.Clear();
			}
		}

		public new void RemoveAt(int Index)
		{
			//lock (List.SyncRoot)
			{
				if (Index < List.Count && Index >= 0)
				{
					Column c = this[Index];
					this.Remove(c);
				}
			}
		}

		public int GetColumnIndex(Column col)
		{
			return col.Index;

			int ToReturn = -1;
			//lock (List.SyncRoot)
			{
				if (this.Contains(col))
				{
					for (int i = 0; i < List.Count; i++)
					{
						if (List[i] == col)
						{
							ToReturn = i;
							break;
						}
					}
				}
			}

			return ToReturn;
		}

		internal int FindLastVisibleColIndex()
		{
			return FindPrevVisibleColIndex(this.Count);
		}

		internal int FindPrevVisibleColIndex(int nInitialIndex)
		{
			int nCurrIndex = nInitialIndex - 1;

			while (nCurrIndex >= 0 && !this[nCurrIndex].Visible)
				nCurrIndex--;

			return nCurrIndex;
		}

		internal int FindNextVisibleColIndex(int nInitialIndex)
		{
			int nCurrIndex = nInitialIndex + 1;

			while (nCurrIndex < List.Count && !this[nCurrIndex].Visible)
				nCurrIndex++;

			return nCurrIndex;
		}
	}

	#region Collection Editor
	/// <summary>
	/// Class created so we can force an invalidation/update on the control when the column editor returns
	/// </summary>
	internal class ColumnCollectionEditor : CollectionEditor
	{
		/// <summary>
		/// Default Constructor for custom column collection editor
		/// </summary>
		/// <param name="type"></param>
		public ColumnCollectionEditor(Type type)
			: base(type)
		{
		}

		/// <summary>
		/// Called to edit a value in collection editor
		/// </summary>
		/// <param name="context"></param>
		/// <param name="isp"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider isp, object value)
		{
			object returnObject = base.EditValue(context, isp, value);

			if (context.Instance is DataList)
			{
				DataList dataList = (DataList)context.Instance;
				// TODO: probably not the best way to handle this...
				dataList.RowWnd.OnColumnAdded(-1);
				dataList.Refresh();
			}

			return returnObject;
		}

		/// <summary>
		/// Creates a new instance of a column for custom collection
		/// </summary>
		/// <param name="itemType"></param>
		/// <returns></returns>
		protected override object CreateInstance(Type ItemType)
		{
			Column col = (Column)base.CreateInstance(ItemType);
			if (this.Context.Instance != null)
			{
				if (this.Context.Instance is DataList)
				{
					col.Parent = ((DataList)this.Context.Instance).Columns;
				}
			}

			col.Text = "New Column";
			return col;
		}

		protected override void DestroyInstance(object instance)
		{
			if (instance is Column)
			{
				((Column)instance).Parent = null;
			}

			base.DestroyInstance(instance);
		}

		// here you can return a text which will be appeared in the list
		// for the given item
		protected override string GetDisplayText(object value)
		{
			if (value is Column)
			{
				Column col = value as Column;
				return string.Format("{0} - {1}", Enum.GetName(typeof(ColumnType), col.Type), col.Text);
			}

			return base.GetDisplayText(value);
		}
	}

	public class ColumnConverter : TypeConverter
	{
		/// <summary>
		/// Required for correct collection editor use
		/// </summary>
		/// <param name="context"></param>
		/// <param name="destinationType"></param>
		/// <returns></returns>
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(InstanceDescriptor))
			{
				return true;
			}
			return base.CanConvertTo(context, destinationType);
		}

		/// <summary>
		/// Required for correct collection editor use
		/// </summary>
		/// <param name="context"></param>
		/// <param name="culture"></param>
		/// <param name="value"></param>
		/// <param name="destinationType"></param>
		/// <returns></returns>
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(InstanceDescriptor) && value is Column)
			{
				ConstructorInfo ci = typeof(Column).GetConstructor(new Type[] { });
				if (ci != null)
				{
					return new InstanceDescriptor(ci, null, false);
				}
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	#endregion
}
