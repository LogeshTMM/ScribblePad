using Microsoft.Win32;
using System.Collections;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
namespace ScribblyPadLibrary {
   public static class Fields {
      #region PRIVATE FIELDS ----------------------------------------        
      public static Line mLine = new (); public static Point mPoint = new ();
      public static bool mIsDrawing, mLeftButtonPressed, mMagicLine, mSingleScribble = true, mMultiScribble,
         mMouseLeave = true, mSingleLine, mConnectedLine, mRectangle, mIntialThemeChange = true,
         mIsFileSaved = false, mIsFileOpened, mWriteTextThemeOnce, mWriteBinThemeOnce, mCircle,
         mEllipse, mDontAddTxt, mDontAddBin; public static Brush mSetStrokeColour = Brushes.White;
      public static readonly Polyline mPolyLine = new ();
      public static PointCollection mPointCollection = new ();
      public static SaveFileDialog mSaveFile = new ();
      public static List<List<UIElement>> mUndoUIElements = new (), mRedoUIElements = new ();
      public static List<UIElement> mUIElements = new (), mDisplayUIElements = new ();
      public static ArrayList mScribbleProperties = new (), mShapeProperties = new ();
      public static int mSetStrokeThickness = 1, mResetCline = 1, mCounter, mUndoCounter,
         mRedoCounter, mTempCounter, mTempSLine = 1;
      public static double mCircleDiameter, mEllipseWidth, mEllipseHeight;
      public static readonly string[] mElements = { "scribble", "sline", "cline", "rect", "circle", "ellipse" };
      public static MessageBoxResult mResult;
      public static string? mDummy = string.Empty;
      #endregion
   }
}