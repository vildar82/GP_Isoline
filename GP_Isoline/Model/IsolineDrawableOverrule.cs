using Autodesk.AutoCAD.GraphicsInterface;
using DB = Autodesk.AutoCAD.DatabaseServices;

namespace GP_Isoline.Model
{
   public class IsolineDrawableOverrule : DrawableOverrule
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