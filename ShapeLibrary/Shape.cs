using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using static ScribblyPadLibrary.Fields;
namespace ShapeLibrary {

   public static class Shape {
      /// <summary>To add the UI elements in the list of list UI elements.</summary>        
      /// /// <param name="element">It's an UI element.</param>        
      public static void AddUIElement (UIElement element) {
         mDisplayUIElements.Add (element);
         mUIElements.Add (element);
         mUndoUIElements.Add (mUIElements);
         mUIElements = new ();
         mUndoCounter++;
      }
   }

   #region CLASS FOR BINARY FILES -----------------------------------------------------------------
   public static class BinaryWriter {

      #region SCRIBBLE (SINGLE & MULTI-SCRIBBLE) --------------------
      /// <summary>This method is utilized to rewrite a Multi-Scribble that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="reader">It has a reference to the current line in the file.</param>
      public static void BinaryWriterScribble (ref string input, ref BinaryReader reader) {
         BinaryFileFormatChecker (ref input, ref reader);
         string[] firstPointArr, secondPointArr;
         try {
            while (true) {
               if (input != null) {
                  if (input != "scribble" && mElements.Contains (input)) break;
                  firstPointArr = input.Split (",");
                  Line line = new () {
                     X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1]),
                     Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
                  };
                  input = reader.ReadString ();
                  if (input != null && input == "scribble") {
                     BinaryFileFormatChecker (ref input, ref reader);
                     firstPointArr = input.Split (",");
                     line = new () {
                        X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1])
                     };
                     input = reader.ReadString ();
                  }
                  if (input != "scribble" && mElements.Contains (input)) break;
                  if (input != null) {
                     secondPointArr = input.Split (",");
                     line.X2 = double.Parse (secondPointArr[0]);
                     line.Y2 = double.Parse (secondPointArr[1]);
                     mUIElements.Add (line);
                  }
               }
            }
         } catch (EndOfStreamException) {
            //MessageBox.Show ($"{ex}");
            input = "";
            return;
         }
      }

      /// <summary>To eliminate the error values (or lines in .bin file) in multiscribble.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private static void BinaryFileFormatChecker (ref string input, ref BinaryReader tempReader) {
         try {
            if (int.TryParse (tempReader.ReadString (), out int value)) mSetStrokeThickness = value;
            if ((input = tempReader.ReadString ()) != null && input[0] == '#')
               mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
            if (mElements.Contains (input)) {
               mDontAddBin = true;
               return;
            }
            while (true) {
               if ((input = tempReader.ReadString ()) == (mDummy = tempReader.ReadString ())) {
                  //if (mIsFileOpened) mUndoCounter++;
                  mTempCounter++;
                  mDontAddBin = false;
                  break;
               } else if (input != "scribble" && mElements.Contains (input)) {
                  mDontAddBin = true;
                  break;
               } else if (mDummy != "scribble" && mElements.Contains (mDummy)) {
                  input = mDummy;
                  mDontAddBin = true;
                  break;
               }
            }
         } catch {
            input = "";
            return;
         }
      }
      #endregion

      #region SINGLE LINE -------------------------------------------
      /// <summary>This method is utilized to rewrite a Single Line that is present in a binary format file.</summary>        
      /// /// <param name="input">It has the key ( or string, let say scribble) to start the method.        
      /// /// Eventually to stop the switch case statement.</param>        
      /// /// <param name="reader">It has a reference to the current line in the file.</param>

      public static void BinaryWriterSingleLine (ref string input, ref BinaryReader reader) {
         try {
            if (mDontAddBin && mDummy == "1") {
               if (int.TryParse (mDummy, out int value)) mSetStrokeThickness = value;
               CreateSline (ref input, ref reader);
            } else if (mDontAddBin && mDummy == "sline") {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateSline (ref input, ref reader);
            }
            for (; input == "sline";) {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateSline (ref input, ref reader);
            }
         } catch {
            input = "";
            return;
         }
      }

      private static void CreateSline (ref string input, ref BinaryReader reader) {
         string firstPoint, secondPoint; string[] firstPointArr, secondPointArr;
         if ((input = reader.ReadString ()) != null && input[0] == '#')
            mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         if ((firstPoint = reader.ReadString ()) != (secondPoint = reader.ReadString ())) {
            firstPointArr = firstPoint.Split (",");
            secondPointArr = secondPoint.Split (",");
            Line line = new () {
               X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1]),
               X2 = double.Parse (secondPointArr[0]), Y2 = double.Parse (secondPointArr[1]),
               Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
            };
            Shape.AddUIElement (line);
            mTempCounter++;
         }
         input = reader.ReadString ();
      }
      #endregion

      #region CONNECTED LINE ----------------------------------------
      /// <summary>This method is utilized to rewrite a Connected Line that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="reader">It has a reference to the current line in the file.</param>
      public static void BinaryWriterConnectedLine (ref string input, ref BinaryReader reader) {
         try {
            if (mDontAddBin && mDummy == "1") {
               if (int.TryParse (mDummy, out int value)) mSetStrokeThickness = value;
               CreateConnectedLine (ref input, ref reader);
            } else if (mDontAddBin && mDummy == "cline") {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateConnectedLine (ref input, ref reader);
            }
            for (; input == "cline";) {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateConnectedLine (ref input, ref reader);
            }
         } catch {
            input = "";
            return;
         }
      }

      private static void CreateConnectedLine (ref string input, ref BinaryReader reader) {
         string firstPoint, secondPoint;
         string[] firstPointArr, secondPointArr;
         if ((input = reader.ReadString ()) != null && input[0] == '#')
            mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         if ((firstPoint = reader.ReadString ()) != (secondPoint = reader.ReadString ())) {
            firstPointArr = firstPoint.Split (",");
            secondPointArr = secondPoint.Split (",");
            Line line = new () {
               X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1]),
               X2 = double.Parse (secondPointArr[0]), Y2 = double.Parse (secondPointArr[1]),
               Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
            };
            Shape.AddUIElement (line);
            mTempCounter++;
         }
         input = reader.ReadString ();
      }
      #endregion

      #region RECTANGLE ---------------------------------------------
      /// <summary>This method is utilized to rewrite a Rectangle that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="reader">It has a reference to the current line in the file.</param>
      public static void BinaryWriteRectangle (ref string input, ref BinaryReader reader) {
         try {
            if (mDontAddBin && mDummy == "1") {
               if (int.TryParse (mDummy, out int value)) mSetStrokeThickness = value;
               CreateRectangle (ref input, ref reader);
            } else if (mDontAddBin && mDummy == "rect") {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateRectangle (ref input, ref reader);
            }
            for (; input == "rect";) {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateRectangle (ref input, ref reader);
            }
         } catch {
            input = "";
            return;
         }
      }

      private static void CreateRectangle (ref string input, ref BinaryReader reader) {
         string firstPoint, secondPoint;
         string[] firstPointArr, secondPointArr;
         double x1, x2, y1, y2;
         if ((input = reader.ReadString ()) != null && input[0] == '#')
            mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         if ((firstPoint = reader.ReadString ()) != (secondPoint = reader.ReadString ())) {
            firstPointArr = firstPoint.Split (","); secondPointArr = secondPoint.Split (",");
            x1 = double.Parse (firstPointArr[0]); x2 = double.Parse (secondPointArr[0]);
            y1 = double.Parse (firstPointArr[1]); y2 = double.Parse (secondPointArr[1]);
            Rectangle rectangle = new () {
               Width = Math.Abs (x2 - x1), Height = Math.Abs (y2 - y1),
               Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
            };
            if (x1 < x2) Canvas.SetLeft (rectangle, x1);
            else Canvas.SetLeft (rectangle, x2);
            if (y1 < y2) Canvas.SetTop (rectangle, y1);
            else Canvas.SetTop (rectangle, y2);
            Shape.AddUIElement (rectangle);
            mTempCounter++;
         }
         input = reader.ReadString ();
      }
      #endregion

      #region CIRCLE ------------------------------------------------
      /// <summary>This method is utilized to rewrite a Circle that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="reader">It has a reference to the current line in the file.</param>
      public static void BinaryWriterCircle (ref string input, ref BinaryReader reader) {
         try {
            if (mDontAddBin && mDummy == "1") {
               if (int.TryParse (mDummy, out int value)) mSetStrokeThickness = value;
               CreateCircle (ref input, ref reader);
            } else if (mDontAddBin && mDummy == "circle") {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateCircle (ref input, ref reader);
            }
            for (; input == "circle";) {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateCircle (ref input, ref reader);
            }
         } catch {
            input = "";
            return;
         }
      }

      private static void CreateCircle (ref string input, ref BinaryReader reader) {
         input = reader.ReadString ();
         if (input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         if (double.TryParse (reader.ReadString (), out double diameter)) {
            Ellipse circle = new () {
               Width = diameter, Height = diameter
            , Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
            };
            Canvas.SetLeft (circle, double.Parse (reader.ReadString ()));
            Canvas.SetTop (circle, double.Parse (reader.ReadString ()));
            Shape.AddUIElement (circle);
            mTempCounter++;
         }
         input = reader.ReadString ();
      }
      #endregion

      #region ELLIPSE -----------------------------------------------
      /// <summary>This method is utilized to rewrite a Ellipse that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      public static void BinaryWriterEllipse (ref string input, ref BinaryReader reader) {
         try {
            if (mDontAddBin && mDummy == "1") {
               if (int.TryParse (mDummy, out int value)) mSetStrokeThickness = value;
               CreateEllipse (ref input, ref reader);
            } else if (mDontAddBin && mDummy == "ellipse") {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateEllipse (ref input, ref reader);
            }
            for (; input == "ellipse";) {
               if (int.TryParse (reader.ReadString (), out int value)) mSetStrokeThickness = value;
               CreateEllipse (ref input, ref reader);
            }
         } catch {
            input = "";
            return;
         }
      }

      private static void CreateEllipse (ref string input, ref BinaryReader reader) {
         input = reader.ReadString ();
         if (input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         Ellipse ellipse = new () {
            Width = double.Parse (reader.ReadString ()),
            Height = double.Parse (reader.ReadString ()), Stroke = mSetStrokeColour
         , StrokeThickness = mSetStrokeThickness
         };
         Canvas.SetLeft (ellipse, double.Parse (reader.ReadString ()));
         Canvas.SetTop (ellipse, double.Parse (reader.ReadString ()));
         Shape.AddUIElement (ellipse);
         mTempCounter++;
         input = reader.ReadString ();
      }
      #endregion
   }
   #endregion


   #region CLASS FOR TEXT FILES -------------------------------------------------------------------

   public static class TextWriter {
      #region SCRIBBLE (SINGLE & MULTI-SCRIBBLE) ------------------        
      /// <summary>This method is utilized to rewrite a Multi-Scribble that is present in a binary format file.</summary>        
      /// /// <param name="input">It has the key ( or string, let say scribble) to start the method.       
      /// /// Eventually to stop the switch case statement.</param>        
      /// /// <param name="tempReader">It has a reference to the current line in the file.</param>        
      public static void TextWriterScribble (ref string? input, ref StreamReader tempReader) {
         TextFileFormatChecker (ref input, ref tempReader);
         if (input == null || input == "" || mDontAddTxt) return;
         string[]? pointArr = input.Split (",");
         while (true) {
            Line line = new () { X1 = double.Parse (pointArr[0]), Y1 = double.Parse (pointArr[1]) };
            if (mElements.Contains (input) || input == null || input == "") break;
            else if (input == "scribble") {
               TextFileFormatChecker (ref input, ref tempReader);
               if (input == null || input == "") break;
               pointArr = input.Split (",");
               line = new () { X1 = double.Parse (pointArr[0]), Y1 = double.Parse (pointArr[1]) };
            }
            input = tempReader.ReadLine ();
            if (input == null || mElements.Contains (input) || input == "") break;
            pointArr = input.Split (",");
            line.X2 = double.Parse (pointArr[0]);
            line.Y2 = double.Parse (pointArr[1]);
            line.Stroke = mSetStrokeColour;
            line.StrokeThickness = mSetStrokeThickness;
            mUIElements.Add (line);
         }
      }

      /// <summary>To eliminate the error values (or lines in .bin file) in multiscribble.</summary>        
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.        
      /// Eventually to stop the switch case statement.</param>        
      ///<param name="tempReader">It has a reference to the current line in the file.</param>        
      private static void TextFileFormatChecker (ref string? input, ref StreamReader tempReader) {
         if (int.TryParse (tempReader.ReadLine (), out int value)) mSetStrokeThickness = value;
         input = tempReader.ReadLine ();
         if (input != null && input[0] == '#')
            mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         if (mElements.Contains (input)) {
            mDontAddTxt = true;
            return;
         }
         while (true) {
            if ((input = tempReader.ReadLine ()) == (mDummy = tempReader.ReadLine ())) {
               //if (mIsFileOpened) mUndoCounter++;
               mTempCounter++;
               mDontAddTxt = false;
               break;
            } else if (input != "scribble" && mElements.Contains (input)) {
               mDontAddTxt = true;
               break;
            } else if (mDummy != "scribble" && mElements.Contains (mDummy)) {
               input = mDummy;
               mDontAddTxt = true;
               break;
            }
         }
      }
      #endregion

      #region SINGLELINE ------------------------------------------
      /// <summary>This method is utilized to rewrite a Single Line that is present in a binary format file.</summary>        
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.        
      /// Eventually to stop the switch case statement.</param>        
      /// <param name="tempReader">It has a reference to the current line in the file.</param>        
      public static void TextWriterSingleLine (ref string? input, ref StreamReader tempReader) {
         if (mDontAddTxt && mDummy == "1") {
            if (int.TryParse (mDummy, out int value)) mSetStrokeThickness = value;
            CreateSingleLine (ref input, ref tempReader);
         }
         for (; input == "sline";) {
            if (int.TryParse (tempReader.ReadLine (), out int thicknessValue)) mSetStrokeThickness = thicknessValue;
            CreateSingleLine (ref input, ref tempReader);
         }
      }
      private static void CreateSingleLine (ref string? input, ref StreamReader tempReader) {
         string? firstPoint, secondPoint;
         string[] firstPointArr, secondPointArr;
         if ((input = tempReader.ReadLine ()) == null) return;
         if (input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         firstPoint = tempReader.ReadLine (); secondPoint = tempReader.ReadLine ();
         if (firstPoint != null && secondPoint != null && secondPoint != "sline" && (firstPoint != secondPoint)) {
            firstPointArr = firstPoint.Split (",");
            secondPointArr = secondPoint.Split (",");
            Line line = new () {
               X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1]),
               X2 = double.Parse (secondPointArr[0]), Y2 = double.Parse (secondPointArr[1]),
               Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
            };
            Shape.AddUIElement (line);
            mTempCounter++;
         }
         _ = secondPoint == "sline" ? input = "sline" : input = tempReader.ReadLine ();
      }
      #endregion

      #region CONNECTED LINE --------------------------------------        
      /// <summary>This method is utilized to rewrite a Connected Line that is present in a binary format file.</summary>        
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.        
      /// Eventually to stop the switch case statement.</param>        
      /// <param name="tempReader">It has a reference to the current line in the file.</param>        
      public static void TextWriterConnectedLine (ref string? input, ref StreamReader tempReader) {
         if (mDontAddTxt && mDummy == "1") {
            if (int.TryParse (mDummy, out int value)) mSetStrokeThickness = value;
            CreateConnectedLine (ref input, ref tempReader);
         }
         for (; input == "cline";) {
            if (int.TryParse (tempReader.ReadLine (), out int value)) mSetStrokeThickness = value;
            CreateConnectedLine (ref input, ref tempReader);
         }
      }
      private static void CreateConnectedLine (ref string? input, ref StreamReader tempReader) {
         string? firstPoint, secondPoint;
         string[] firstPointArr, secondPointArr;
         if ((input = tempReader.ReadLine ()) != null && input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         if ((firstPoint = tempReader.ReadLine ()) != null && (secondPoint = tempReader.ReadLine ()) != null && (firstPoint != secondPoint)) {
            firstPointArr = firstPoint.Split (",");
            secondPointArr = secondPoint.Split (",");
            Line line = new () {
               X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1]),
               X2 = double.Parse (secondPointArr[0]), Y2 = double.Parse (secondPointArr[1]),
               Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
            };
            Shape.AddUIElement (line);
            mTempCounter++;
         }
         input = tempReader.ReadLine ();
      }
      #endregion

      #region RECTANGLE -------------------------------------------
      /// <summary>This method is utilized to rewrite a Rectangle that is present in a binary format file.</summary>        
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.        
      /// Eventually to stop the switch case statement.</param>        
      /// <param name="tempReader">It has a reference to the current line in the file.</param>       
      public static void TextWriterRectangle (ref string? input, ref StreamReader tempReader) {
         if (mDontAddTxt && mDummy == "1") {
            if (int.TryParse (mDummy, out int value)) mSetStrokeThickness = value;
            CreateRectangle (ref input, ref tempReader);
         }
         for (; input == "rect";) {
            if (int.TryParse (tempReader.ReadLine (), out int value)) mSetStrokeThickness = value;
            CreateRectangle (ref input, ref tempReader);
         }
      }
      private static void CreateRectangle (ref string? input, ref StreamReader tempReader) {
         string? firstPoint, secondPoint; string[] firstPointArr, secondPointArr;
         double x1, x2, y1, y2;
         input = tempReader.ReadLine ();
         if (input != null && input[0] == '#')
            mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         firstPoint = tempReader.ReadLine (); secondPoint = tempReader.ReadLine ();
         if (firstPoint != null && secondPoint != null && secondPoint != "rect") {
            firstPointArr = firstPoint.Split (",");
            secondPointArr = secondPoint.Split (",");
            x1 = double.Parse (firstPointArr[0]); y1 = double.Parse (firstPointArr[1]);
            x2 = double.Parse (secondPointArr[0]); y2 = double.Parse (secondPointArr[1]);
            Rectangle rectangle = new () {
               Width = Math.Abs (x2 - x1), Height = Math.Abs (y2 - y1),
               Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
            };
            if (x1 < x2) Canvas.SetLeft (rectangle, x1);
            else Canvas.SetLeft (rectangle, x2);
            if (y1 < y2) Canvas.SetTop (rectangle, y1);
            else Canvas.SetTop (rectangle, y2);
            Shape.AddUIElement (rectangle);
            mTempCounter++;
         }
         _ = secondPoint == "rect" ? input = "rect" : input = tempReader.ReadLine ();
      }
      #endregion

      #region CIRCLE ----------------------------------------------
      /// <summary>This method is utilized to rewrite a Circle that is present in a binary format file.</summary>        
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.        
      /// Eventually to stop the switch case statement.</param>        
      ///<param name="tempReader">It has a reference to the current line in the file.</param>        
      public static void TextWriterCircle (ref string? input, ref StreamReader tempReader) {
         if (mDontAddTxt && mDummy == "1") {
            if (int.TryParse (mDummy, out int value)) mSetStrokeThickness = value;
            CreateCircle (ref input, ref tempReader);
         }
         for (; input == "circle";) {
            if (int.TryParse (tempReader.ReadLine (), out int value)) mSetStrokeThickness = value;
            CreateCircle (ref input, ref tempReader);
         }
      }
      private static void CreateCircle (ref string? input, ref StreamReader tempReader) {
         input = tempReader.ReadLine ();
         if (input != null && input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         if (double.TryParse (tempReader.ReadLine (), out double diameter)) {
            Ellipse circle = new () {
               Width = diameter, Height = diameter,
               Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
            };
            if (double.TryParse (tempReader.ReadLine (), out double left)) Canvas.SetLeft (circle, left);
            if (double.TryParse (tempReader.ReadLine (), out double top)) Canvas.SetTop (circle, top);
            Shape.AddUIElement (circle);
            mTempCounter++;
         }
         input = tempReader.ReadLine ();
      }
      #endregion

      #region ELLIPSE ---------------------------------------------        
      /// <summary>This method is utilized to rewrite a Ellipse that is present in a binary format file.</summary>        
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.        
      /// Eventually to stop the switch case statement.</param>        
      ///<param name="tempReader">It has a reference to the current line in the file.</param>        
      public static void TextWriterEllipse (ref string? input, ref StreamReader tempReader) {
         if (mDontAddTxt && mDummy == "1") {
            if (int.TryParse (tempReader.ReadLine (), out int value)) mSetStrokeThickness = value;
            CreateEllipse (ref input, ref tempReader);
         }
         for (; input == "ellipse";) {
            if (int.TryParse (tempReader.ReadLine (), out int value)) mSetStrokeThickness = value;
            CreateEllipse (ref input, ref tempReader);
         }
      }

      private static void CreateEllipse (ref string? input, ref StreamReader tempReader) {
         input = tempReader.ReadLine (); if (input != null && input[0] == '#')
            mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         if (double.TryParse (tempReader.ReadLine (), out double eWidth) && double.TryParse (tempReader.ReadLine (), out double eHeight)) {
            Ellipse ellipse = new () {
               Width = eWidth, Height = eHeight,
               Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
            };
            if (double.TryParse (tempReader.ReadLine (), out double left)) Canvas.SetLeft (ellipse, left);
            if (double.TryParse (tempReader.ReadLine (), out double top)) Canvas.SetTop (ellipse, top);
            Shape.AddUIElement (ellipse);
            mTempCounter++;
            input = tempReader.ReadLine ();
         }
      }
      #endregion
   }
   #endregion
}