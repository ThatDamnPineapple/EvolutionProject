using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace EvoSim.Helpers.HelperClasses
{
    public class IndexedInstance<T>
    {
        public T obj;

        public Point position;

        public IndexedInstance(T item, Point pos)
        {
            obj = item;
            position = pos;
        }

        public override bool Equals(object compare)
        {
            if (compare is not IndexedInstance<T>)
                return false;
            IndexedInstance<T> cast = compare as IndexedInstance<T>;
            if (cast.obj is T castObj)
            {
                if (cast.position != position)
                    return false;
                if (castObj.Equals(obj))
                    return true;
            }
            return false;
        }
    }

    public class BasicPList<T>
    {
        public List<IndexedInstance<T>> list;

        public List<T> basicList;

        public BasicPList()
        {
            list= new List<IndexedInstance<T>>();
            basicList= new List<T>();
        }

        public T this[int index]
        {
            get
            {
                return list[index].obj;
            }

            set
            {
                list[index].obj = value;
            }
        }

        public bool Remove(T item)
        {
            List<IndexedInstance<T>> toRemove = new List<IndexedInstance<T>>();
            int listCount = list.Count();
            for (int i = 0; i < list.Count(); i++)
            {
                IndexedInstance<T> II = list[i];
                object o = II.obj;
                if (o.Equals(item))
                {
                    toRemove.Add(II);
                }
            }

            toRemove.ForEach(n => list.Remove(n));

            basicList.Remove(item);
            return true;
        }

        public void Remove(IndexedInstance<T> instance)
        {
            int listCount = list.Count();
            for (int i = 0; i < list.Count(); i++)
            {
                IndexedInstance<T> II = list[i];
                if (instance.Equals(II))
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        public bool Contains(T item)
        {
            return basicList.Contains(item);
        }

        public void Clear()
        {
            list.Clear();
            basicList.Clear();
        }

        public int Count()
        {
            return list.Count();
        }

        public int CountNoDuplicates()
        {
            return basicList.Count();
        }

        public void Add(T item, Point index)
        {
            IndexedInstance<T> newInstance = new IndexedInstance<T>(item, index);
            list.Add(newInstance);

            if (!basicList.Contains(item))
                basicList.Add(item);
        }

        public void CullDuplicates(IndexedInstance<T> instance)
        {
            List<IndexedInstance<T>> toRemove = list.Where(n => instance.Equals(instance)).ToList();
            toRemove.ForEach(n => list.Remove(n));
        }
    }

    public abstract class PartitionedList<T>
    {
        public delegate void ListAction(ref List<T> item, Point index);

        public List<T>[,] list;

        public BasicPList<T> centralList;

        public List<T> basicList => centralList.basicList;

        public int Rows;
        public int Columns;

        public bool IsReadOnly => false;
        public bool IsFixedSize => false;

        public PartitionedList(int rows, int columns)
        {
            list = new List<T>[rows, columns];
            Rows = rows;
            Columns = columns;
            InvokeInEverySector(new ListAction((ref List<T> subList, Point testPoint) =>
            {
                subList = new List<T>();
            }));
            centralList = new BasicPList<T>();
        }

        public void InvokeInEverySector(ListAction action)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    action.Invoke(ref list[i, j], new Point(i,j));
                }
            }
        }

        public Point ClampPoint(Point point)
        {
            point.X %= Rows;
            point.Y %= Columns;

            while (point.X < 0)
                point.X += Rows;

            while (point.Y < 0)
                point.Y += Columns;
            return point;
        }

        public void InvokeInAdjacentSectors(ListAction action, Point point, bool includeOrigin)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0 && !includeOrigin) continue;
                    Point newPoint = new Point(point.X + i, point.Y + j);
                    newPoint = ClampPoint(newPoint);
                    action.Invoke(ref list[newPoint.X, newPoint.Y], newPoint);
                }
            }
        }

        public List<T> GetListBasic(int row, int column)
        {
            return list[row, column];
        }

        public List<T> GetListBasic(Point point)
        {
            return GetListBasic(point.X, point.Y);
        }

        public void AddToSpecificList(T obj, Point point)
        {
            GetListBasic(point).Add(obj);
        }

        public void RemoveFromSpecificList(T obj, Point point)
        {
            GetListBasic(point).Remove(obj);
        }

        public void AddToSpecificList(T obj, int row, int column)
        {
            GetListBasic(row, column).Add(obj);
        }

        public void RemoveFromSpecificList(T obj, int row, int column)
        {
            GetListBasic(row, column).Remove(obj);
        }

        public virtual void AddToCorrectList(T obj)
        {
            throw new NotImplementedException();
        }

        public virtual Point GetListID(T item)
        {
            throw new NotImplementedException();
        }

        public virtual bool ValidInList(T obj, Point index)
        {
            throw new NotImplementedException();
        }

        public virtual void Add(T obj)
        {
            if (!centralList.Contains(obj))
            {
                AddToCorrectList(obj);
                centralList.Add(obj, GetListID(obj));
            }
        }

        public void UpdateInstanceList(List<IndexedInstance<T>> instances, IndexedInstance<T> instance, T item)
        {
            if (instance.obj.Equals(item))
                instances.Add(instance);
        }

        public void RemoveInstanceList(List<IndexedInstance<T>> instances)
        {
            instances.ForEach(n => RemoveFromSpecificList(n.obj, n.position));
        }

        public bool Remove(T item)
        {
            if (!centralList.Contains(item))
                return false;

            List<IndexedInstance<T>> instances = new List<IndexedInstance<T>>();
            centralList.list.ForEach(n => UpdateInstanceList(instances, n, item));
            RemoveInstanceList(instances);
            centralList.Remove(item);
            InvokeInEverySector(new ListAction((ref List<T> subList, Point testPoint) =>
            {
                subList.Remove(item);
            }));
            return true;
        }

        public bool Contains(T obj)
        {
            return centralList.Contains(obj);
        }

        public void Clear()
        {
            centralList.list.Clear();
            InvokeInEverySector(new ListAction((ref List<T> subList, Point testPoint) =>
            {
                subList = new List<T>();
            }));
        }

        public int Count()
        {
            return centralList.CountNoDuplicates();
        }
    }

    public abstract class PartitionedPositionList<T> : PartitionedList<T>
    {

        public PartitionedPositionList(int rows, int columns) : base(rows, columns)
        {

        }

        public override Point GetListID(T item)
        {
            return GetListID(GetPos(item));
        }

        public Vector2 ToWorldCoords(Point point)
        {
            Vector2 ret = Vector2.Zero;
            ret.X = (point.X * SceneManager.grid.mapSize.X) / (float)Rows;
            ret.Y = (point.Y * SceneManager.grid.mapSize.Y) / (float)Columns;
            return ret;
        }

        public virtual Point GetListID(Vector2 pos)
        {
            int row = (int)((pos.X / SceneManager.grid.mapSize.X) * Rows);
            int column = (int)((pos.Y / SceneManager.grid.mapSize.Y) * Columns);
            Point ret = new Point(row, column);
            return ClampPoint(ret);
        }

        public virtual List<T> GetList(Vector2 pos)
        {
            return GetListBasic(GetListID(pos));
        }

        public virtual Vector2 GetPos(T obj)
        {
            return Vector2.Zero;
        }

        public virtual void UpdateObjPos(T obj)
        {
            throw new NotImplementedException();
        }

        public void UpdateObjPos(T obj, Vector2 oldPos, Vector2 newPos)
        {
            Point oldID = GetListID(oldPos);
            Point newID = GetListID(newPos);
            if (oldID != newID)
            {
                List<T> newList = GetListBasic(newID);
                if (!newList.Contains(obj))
                {
                    newList.Add(obj);
                }

            }

            ListAction action = new ListAction((ref List<T> subList, Point testPoint) =>
            {
                if (!ValidInList(obj, testPoint))
                {
                    centralList.Remove(new IndexedInstance<T>(obj, testPoint));
                    subList.Remove(obj);
                }
            });

            InvokeInAdjacentSectors(action, GetListID(obj), false);
        }

        public override void AddToCorrectList(T obj)
        {
            Vector2 pos = GetPos(obj);
            Point point = GetListID(pos);

            InvokeInAdjacentSectors(new ListAction((ref List<T> subList, Point testPoint) =>
            {
                AddToSpecificList(obj, testPoint.X, testPoint.Y);
            }), point, true);
        }
    }
}
