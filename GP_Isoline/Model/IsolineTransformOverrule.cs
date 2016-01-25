using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace GP_Isoline.Model
{
   public class IsolineTransformOverrule : TransformOverrule
   {
      public IsolineTransformOverrule()
      {
         SetXDataFilter(Isoline.RegAppNAME);
      }

      public override void Explode(Entity entity, DBObjectCollection entitySet)
      {
         Polyline pl = entity as Polyline;
         if (pl!=null)
         {
            Isoline isoline = new Isoline(pl);
            if (isoline.IsIsoline)
            {
               var lines = isoline.GetLines(pl);
               foreach (var line in lines)
               {
                  entitySet.Add(line);
               }               
            }
         }
      }
   }
}
