using EvoSim.Helpers.HelperClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvoSim.ProjectContent.CellStuff
{
    public class PartitionedCellList<T> : PartitionedPositionList<T> where T : Cell //Universally based off of CENTER
    {
        public PartitionedCellList(int rows, int columns) : base(rows, columns)
        {

        }

        public override Point GetListID(Vector2 pos)
        {
            return base.GetListID(pos);
        }

        public override bool ValidInList(T cell, Point index)
        {
            Vector2 comparison = ToWorldCoords(index);
            if (MathF.Abs(comparison.X - cell.Center.X) > cell.width || MathF.Abs(comparison.Y - cell.Center.Y) > cell.height)
            {
                return false;
            }
            return true;
        }

        public override List<T> GetList(Vector2 pos)
        {
            return base.GetList(pos);
        }

        public override Vector2 GetPos(T cell)
        {
            return cell.Center;
        }

        public override void UpdateObjPos(T cell)
        {
            UpdateObjPos(cell, cell.oldCenter, cell.Center);
        }
    }
}
