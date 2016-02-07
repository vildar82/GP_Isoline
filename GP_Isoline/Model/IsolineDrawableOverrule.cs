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

         DB.Curve curve = drawable as DB.Curve;
         if (curve != null)
         {
            Isoline isoline = new Isoline(curve);
            if (isoline.IsIsoline)
            {
               var lines = isoline.GetLines(curve);
               foreach (var line in lines)
               {
                  wd.Geometry.Draw(line);
               }
            }
         }

         // return the base
         return true;
      }
   }
}