using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB = Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace GP_Isoline.Model
{
   public class IsolineDrawableOverrule: DrawableOverrule
   {
      public IsolineDrawableOverrule()
      {
         SetXDataFilter(Isoline.RegAppNAME);
      }

      public override bool WorldDraw(Drawable drawable, WorldDraw wd)
      {
         // draw the base class
         bool ret = base.WorldDraw(drawable, wd);         

         DB.Polyline pl = drawable as DB.Polyline;
         if (pl != null)
         {
            Isoline isoline = new Isoline(pl);
            if (isoline.IsIsoline)
            {
               var lines = isoline.GetLines(pl);
               foreach (var line in lines)
               {
                  wd.Geometry.Draw(line);
               }                              
            }         
         }       

         // return the base
         return ret;
      }
   }
}
