using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;


namespace ScribblyPad {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      public MainWindow () {
         InitializeComponent ();
         CurrentLocation.Text = Environment.CurrentDirectory;
      }

      #region CORE EVENTS for SCRIBBLE and SHAPES -------------------   
      private void Canvas_MouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
         if (mIntialThemeChange) ThemeChange ();
         if (!mConnectedLine) EscapeButton.IsEnabled = false;

         if (mSingleScribble && sender is Canvas) {
            ScribblyRegion.Children.Clear ();
            mScribbleProperties.Clear ();
            ScribbleInfo ();
         } else if (mMultiScribble && sender is Canvas) {
            ScribbleInfo ();
            if (mIsFileSaved) mIsFileSaved = false;
         }

         if (mSingleLine || mRectangle) {
            if (mIsFileSaved) mIsFileSaved = false;
            _ = mSingleLine ? mShapeProperties.Add ("sline") : mShapeProperties.Add ("rect");
            mShapeProperties.Add (mSetStrokeThickness);
            mShapeProperties.Add (mSetStrokeColour);
            mPoint = e.GetPosition (ScribblyRegion);
            mShapeProperties.Add (mPoint);
            mLine = new () {
               X1 = mPoint.X, Y1 = mPoint.Y
            };
         } else if (mConnectedLine) {
            if (mIsFileSaved) mIsFileSaved = false;
            if (mResetCline == 1) { // mResetCline = 1 indicates when you press escape context menu after that
                                    // you want to draw cline again.Then it will not start draw a line with
                                    // the help of previous end point of cline.
               mLine = new ();
               mPoint = e.GetPosition (ScribblyRegion);
               mResetCline = 0;
            } else mLine = new ();
            mShapeProperties.Add ("cline");
            mShapeProperties.Add (mSetStrokeThickness);
            mShapeProperties.Add (mSetStrokeColour);
            mShapeProperties.Add (mPoint);
            mLine.X1 = mPoint.X;
            mLine.Y1 = mPoint.Y;
         }

         if ((mSingleScribble || mMultiScribble) && sender is Canvas) {
            mIsDrawing = true;
            mPoint = e.GetPosition (ScribblyRegion);
         } else if (mMagicLine == true && sender is Canvas) {
            mScribbleProperties.Clear ();
            ScribblyRegion.Children.Clear ();
            mLeftButtonPressed = true;
            mIsDrawing = false;
            mPointCollection.Clear ();
         }
         mMouseLeave = false;
      }

      /// <summary>To add the information in ArrayList to identify the operation in scribblypad.</summary>
      private void ScribbleInfo () {
         mScribbleProperties.Add ("scribble");
         mScribbleProperties.Add (mSetStrokeThickness);
         mScribbleProperties.Add (mSetStrokeColour);
      }

      private void ScribblePad_MouseMove (object sender, MouseEventArgs e) {
         if (mIntialThemeChange) ThemeChange ();
         if (!mConnectedLine) EscapeButton.IsEnabled = false;

         if (mIsDrawing || mLeftButtonPressed || mSingleLine || mConnectedLine || mRectangle || mCircle || mEllipse) {
            XPosition.Text = Convert.ToString ((int)mPoint.X);
            YPosition.Text = Convert.ToString ((int)mPoint.Y);
            SetStatus.Text = "Active";
            mSetStrokeThickness = Convert.ToInt32 (SilderValue.Value);
            SetStatus.Foreground = Brushes.Green;
         }
         if (mIsDrawing && sender is Canvas && !mMouseLeave) {
            mLine = new () { X1 = mPoint.X, Y1 = mPoint.Y };
            mScribbleProperties.Add (mPoint);
            mPoint = e.GetPosition (ScribblyRegion);
            mLine.X2 = mPoint.X;
            mLine.Y2 = mPoint.Y;
            mScribbleProperties.Add (mPoint);
            mLine.Stroke = mSetStrokeColour;
            mLine.StrokeThickness = mSetStrokeThickness;
            ScribblyRegion.Children.Add (mLine);
            if (mIsFileSaved) mIsFileSaved = false;
            mUIElements.Add (mLine); // scribble have many line object in one scribble so we add list of uielement
                                     // mUIElement and leftbutton up event add the whole number of mLine object 
                                     // let say minimun 250 plus in list of list of uielement mUndoUIElement.
         } else if (mLeftButtonPressed == true && mMagicLine && sender is Canvas && !mMouseLeave) {
            mPoint = e.GetPosition (ScribblyRegion);
            mPointCollection.Add (mPoint);
            mPolyLine.Points = mPointCollection;
            mPolyLine.Stroke = mSetStrokeColour;
            mPolyLine.StrokeThickness = mSetStrokeThickness;
         } else return;

      }

      private void ScribblePad_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
         mIsDrawing = false;
         mLeftButtonPressed = false;
         SetStatus.Foreground = Brushes.Red;
         SetStatus.Text = "Inactive";
         if (sender is Canvas && mMagicLine == true) ScribblyRegion.Children.Add (mPolyLine);
         else if ((mSingleLine || mRectangle) && !mMouseLeave) {
            mPoint = e.GetPosition (ScribblyRegion);
            mShapeProperties.Add (mPoint);
            mLine.X2 = mPoint.X;
            mLine.Y2 = mPoint.Y;
            if (mRectangle && !mMouseLeave && mLine.X1 != mLine.X2 && mLine.Y1 != mLine.Y2) {
               Rectangle rect = new () {
                  Width = Math.Abs (mLine.X2 - mLine.X1), Height = Math.Abs (mLine.Y2 - mLine.Y1),
                  Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
               };
               if (mLine.X1 < mLine.X2) Canvas.SetLeft (rect, mLine.X1);
               else Canvas.SetLeft (rect, mLine.X2);
               if (mLine.Y2 < mLine.Y1) Canvas.SetTop (rect, Math.Abs (mLine.Y2));
               else Canvas.SetTop (rect, Math.Abs (mLine.Y1));
               ScribblyRegion.Children.Add (rect);
               AddUIElement (rect);
            } else if (!mRectangle) {
               mLine.Stroke = mSetStrokeColour;
               mLine.StrokeThickness = mSetStrokeThickness;
               ScribblyRegion.Children.Add (mLine);
               AddUIElement (mLine);
            }
         } else if (mConnectedLine) {
            mPoint = e.GetPosition (ScribblyRegion);
            mLine.X2 = mPoint.X;
            mLine.Y2 = mPoint.Y;
            mShapeProperties.Add (mPoint);
            mLine.Stroke = mSetStrokeColour;
            mLine.StrokeThickness = mSetStrokeThickness;
            ScribblyRegion.Children.Add (mLine);
            AddUIElement (mLine);
         } else if (mCircle) {
            if (mCircleDiameter < this.Height) {
               Ellipse circle = new () {
                  Width = mCircleDiameter, Height = mCircleDiameter,
                  Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
               };
               mPoint = e.GetPosition (ScribblyRegion);
               if (!mMouseLeave && mPoint.Y > (mCircleDiameter / 2)) {
                  Canvas.SetLeft (circle, mPoint.X - (circle.Width / 2));
                  Canvas.SetTop (circle, mPoint.Y - (circle.Width / 2));
                  ScribblyRegion.Children.Add (circle);
                  mShapeProperties.Add ("circle");
                  mShapeProperties.Add (mSetStrokeThickness);
                  mShapeProperties.Add (mSetStrokeColour);
                  mShapeProperties.Add (mCircleDiameter);
                  mShapeProperties.Add (mPoint.X - (circle.Width / 2));
                  mShapeProperties.Add (mPoint.Y - (circle.Width / 2));
                  AddUIElement (circle);
               } else MessageBox.Show ("There is no enough space to insert a circle");
            } else MessageBox.Show ("There is no enough space to insert a circle");
         } else if (mEllipse) {
            if (mEllipseWidth < Width && mEllipseHeight < Height) {
               Ellipse ellipse = new () {
                  Width = mEllipseWidth, Height = mEllipseHeight,
                  Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
               };
               mPoint = e.GetPosition (ScribblyRegion);
               if (!mMouseLeave && mPoint.Y > (mEllipseHeight / 2)) {
                  Canvas.SetLeft (ellipse, mPoint.X - mEllipseWidth / 2);
                  Canvas.SetTop (ellipse, mPoint.Y - mEllipseHeight / 2);
                  ScribblyRegion.Children.Add (ellipse);
                  mShapeProperties.Add ("ellipse");
                  mShapeProperties.Add (mSetStrokeThickness);
                  mShapeProperties.Add (mSetStrokeColour);
                  mShapeProperties.Add (mEllipseWidth);
                  mShapeProperties.Add (mEllipseHeight);
                  mShapeProperties.Add (mPoint.X - mEllipseWidth / 2);
                  mShapeProperties.Add (mPoint.Y - mEllipseWidth / 2);
                  AddUIElement (ellipse);
               } else MessageBox.Show ("There is no enough space to insert an ellipse");
            } else MessageBox.Show ("There is no enough space to insert an ellipse");
         }

         if (mSingleScribble || mMultiScribble) {
            mUndoUIElements.Add (mUIElements);
            mUIElements = new ();
            mUndoCounter++;
         }
      }

      private void ScribblePad_MouseLeave (object sender, MouseEventArgs e) =>
         mMouseLeave = true;

      /// <summary>To add the UI elements in the list of list UI elements.</summary>
      /// <param name="element">It's an UI element.</param>
      private void AddUIElement (UIElement element) {
         mUIElements.Add (element);
         mUndoUIElements.Add (mUIElements);
         mUIElements = new ();
         mUndoCounter++;
      }
      #endregion

      #region FILE NEW EVENT ----------------------------------------
      private void New_Click (object sender, RoutedEventArgs e) {
         if (!mIsFileSaved && (mScribbleProperties.Count != 0 || mShapeProperties.Count != 0)) {
            mResult = MessageBox.Show ("Do you want to save this file?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (mResult == MessageBoxResult.Yes) {
               CreateDotTextFile ();
               NewFileSetter ();
            } else if (mResult == MessageBoxResult.No) NewFileSetter ();
         } else if (mIsFileSaved && (mSingleScribble || mMultiScribble || mMagicLine || mSingleLine
            || mConnectedLine || mRectangle || mCircle || mRectangle)) NewFileSetter ();
         else if (mIsFileSaved && (!mSingleScribble || !mMultiScribble || !mMagicLine ||
            !mSingleLine || !mConnectedLine || !mRectangle)) NewFileSetter ();
         else if (!mIsFileSaved && mSingleScribble) NewFileSetter ();
      }

      private void NewFileSetter () {
         Title = "Scribbly Pad";
         Clear ();
         mSaveFile.InitialDirectory = CurrentLocation.Text = Environment.CurrentDirectory;
         mSingleScribble = true;
         XPosition.Text = YPosition.Text = "0";
         mMultiScribble = mMagicLine = mSingleLine = mConnectedLine = mRectangle = mCircle = mEllipse = false;
         CurrentOperation.Text = "Single Scribble";
      }
      #endregion

      #region COMMAND BINDING FOR FILE OPEN -------------------------
      private void Open_CanExecute (object sender, CanExecuteRoutedEventArgs e)
         => e.CanExecute = true;

      private void Open_Executed (object sender, ExecutedRoutedEventArgs e) {
         OpenFileDialog openFile = new ();
         if (openFile.ShowDialog () == true) {
            string fileExetension = Path.GetExtension (openFile.FileName);
            CurrentLocation.Text = openFile.FileName;
            mUndoCounter = 0; mRedoCounter = 0;
            mUndoUIElements.Clear (); mRedoUIElements.Clear ();
            mIsFileOpened = true;
            try {
               FileStream fileStream = new (openFile.FileName, FileMode.OpenOrCreate);
               if (fileExetension == ".txt") {
                  StreamReader reader = new (fileStream);
                  string? point = reader.ReadLine ();
                  if (point != null && point[0] == '#')
                     ScribblyRegion.Background = (SolidColorBrush)new BrushConverter ().ConvertFrom (point)!;
                  else throw new Exception ("It's not a Scribbly Pad file.");
                  point = reader.ReadLine ();
                  if (point != null && point[0] == '#') {
                     Changer.Fill = mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (point)!;
                     Changer.ToolTip = "Black";
                  }
                  point = reader.ReadLine ();
                  while (true) {
                     switch (point) {
                        case "scribble":
                           TextWriteMultiScribble (ref point, ref reader);
                           mUndoUIElements.Add (mUIElements);
                           mUIElements = new ();
                           break;
                        case "sline":
                           TextWriteSingleLine (ref point, ref reader);
                           break;

                        case "cline":
                           TextWriteConnectedLine (ref point, ref reader);
                           break;

                        case "rect":
                           TextWriteRectangle (ref point, ref reader);
                           break;

                        case "circle":
                           TextWriteCircle (ref point, ref reader);
                           break;

                        case "ellipse":
                           TextWriteEllipse (ref point, ref reader);
                           break;

                        default: break;
                     }
                     if (point == null) break; // Don't remove null because ReadString() returns null when it reached end of the file.
                  }

               } else if (fileExetension == ".bin") {
                  BinaryReader reader = new (fileStream);
                  string point = reader.ReadString ();
                  while (true) {
                     switch (point) {
                        case "scribble":
                           BinaryWriteMultiScribble (ref point, ref reader);
                           mUndoUIElements.Add (mUIElements);
                           mUIElements = new ();
                           break;
                        case "sline":
                           BinaryWriteSingleLine (ref point, ref reader);
                           break;
                        case "cline":
                           BinaryWriteConnectedLine (ref point, ref reader);
                           break;
                        case "rect":
                           BinaryWriteRectangle (ref point, ref reader);
                           break;
                        case "circle":
                           BinaryWriteCircle (ref point, ref reader);
                           break;
                        case "ellipse":
                           BinaryWriteEllipse (ref point, ref reader);
                           break;
                        default: break;
                     }
                     if (point == "") break;
                  }
               } else MessageBox.Show ("Invaild File Extension");
               Title = Path.GetFileNameWithoutExtension (openFile.FileName);
            } catch (Exception ex) {
               MessageBox.Show (ex.Message);
            }
         }
      }
      #endregion

      #region FILE OPEN, SAVE and SAVE AS Methods -------------------

      #region To Open .txt format and TextWrite Methods for Multi-Scribble and all Shapes

      /// <summary>This method is utilized to rewrite a Multi-Scribble that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void TextWriteMultiScribble (ref string? point, ref StreamReader reader) {
         TextFileFormatChecker (ref point, ref reader);
         if (point == null || point == "") return;
         string[]? pointArr = point.Split (",");
         while (true) {
            Line line = new () { X1 = double.Parse (pointArr[0]), Y1 = double.Parse (pointArr[1]) };
            if (mElements.Contains (point = reader.ReadLine ()) || point == null) break;
            else if (point == "scribble") {
               TextFileFormatChecker (ref point, ref reader);
               if (point == null || point == "") break;
               pointArr = point.Split (",");
               line = new () { X1 = double.Parse (pointArr[0]), Y1 = double.Parse (pointArr[1]) };
               point = reader.ReadLine ();
            }
            if (point == null || point == "") break;
            pointArr = point.Split (",");
            line.X2 = double.Parse (pointArr[0]);
            line.Y2 = double.Parse (pointArr[1]);
            line.Stroke = mSetStrokeColour;
            line.StrokeThickness = mSetStrokeThickness;
            ScribblyRegion.Children.Add (line);
            mUIElements.Add (line);
         }
      }

      /// <summary>To eliminate the error values (or lines in .bin file) in multiscribble.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void TextFileFormatChecker (ref string? input, ref StreamReader tempReader) {
         if (int.TryParse (tempReader.ReadLine (), out int value)) mSetStrokeThickness = value;
         if ((input = tempReader.ReadLine ()) != null && input[0] == '#')
            mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         while (true)
            if ((input = tempReader.ReadLine ()) == (_ = tempReader.ReadLine ())) break;
         if (mIsFileOpened) mUndoCounter++;
         mTempCounter++;
      }

      /// <summary>This method is utilized to rewrite a Single Line that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void TextWriteSingleLine (ref string? input, ref StreamReader tempReader) {
         string? firstPoint, secondPoint;
         string[] pointArr;
         for (; input == "sline";) {
            if (int.TryParse (tempReader.ReadLine (), out int thicknessValue)) mSetStrokeThickness = thicknessValue;
            if ((input = tempReader.ReadLine ()) == null) break;
            if (input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
            if (((firstPoint = tempReader.ReadLine ()) != (secondPoint = tempReader.ReadLine ())) && secondPoint != "sline") {
               if (firstPoint != null && secondPoint != null) {
                  pointArr = firstPoint.Split (",");
                  Line line = new () {
                     X1 = double.Parse (pointArr[0]), Y1 = double.Parse (pointArr[1]),
                     Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
                  };
                  pointArr = secondPoint.Split (",");
                  line.X2 = double.Parse (pointArr[0]);
                  line.Y2 = double.Parse (pointArr[1]);
                  ScribblyRegion.Children.Add (line);
                  AddUIElement (line);
                  mTempCounter++;
               }
            }
            _ = secondPoint == "sline" ? input = "sline" : input = tempReader.ReadLine ();
         }
      }

      /// <summary>This method is utilized to rewrite a Connected Line that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void TextWriteConnectedLine (ref string? input, ref StreamReader tempreader) {
         string? firstPoint, secondPoint;
         string[] firstPointArr, secondPointArr;
         for (; input == "cline";) {
            if (int.TryParse (tempreader.ReadLine (), out int value)) mSetStrokeThickness = value;
            if ((input = tempreader.ReadLine ()) != null && input[0] == '#')
               mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
            if ((firstPoint = tempreader.ReadLine ()) != null && (secondPoint = tempreader.ReadLine ()) != null) {
               firstPointArr = firstPoint.Split (",");
               secondPointArr = secondPoint.Split (",");
               Line line = new () {
                  X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1]),
                  X2 = double.Parse (secondPointArr[0]), Y2 = double.Parse (secondPointArr[1]),
                  Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
               };
               ScribblyRegion.Children.Add (line);
               AddUIElement (line);
               mTempCounter++;
               input = tempreader.ReadLine ();
            }
         }
      }

      /// <summary>This method is utilized to rewrite a Rectangle that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void TextWriteRectangle (ref string? input, ref StreamReader tempReader) {
         string? firstPoint, secondPoint;
         string[] firstPointArr, secondPointArr;
         double x1, x2, y1, y2;
         for (; input == "rect";) {
            if (int.TryParse (tempReader.ReadLine (), out int value)) mSetStrokeThickness = value;
            if ((input = tempReader.ReadLine ()) != null && input[0] == '#')
               mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
            if ((firstPoint = tempReader.ReadLine ()) != (secondPoint = tempReader.ReadLine ()) &&
               firstPoint != null && secondPoint != null) {
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
               ScribblyRegion.Children.Add (rectangle);
               AddUIElement (rectangle);
               mTempCounter++;
            }
            input = tempReader.ReadLine ();
         }
      }

      /// <summary>This method is utilized to rewrite a Circle that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void TextWriteCircle (ref string? input, ref StreamReader tempReader) {
         for (; input == "circle";) {
            if (int.TryParse (tempReader.ReadLine (), out int value)) mSetStrokeThickness = value;
            input = tempReader.ReadLine ();
            if (input != null && input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
            if (double.TryParse (tempReader.ReadLine (), out double diameter)) {
               Ellipse circle = new () {
                  Width = diameter, Height = diameter
               , Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
               };
               if (double.TryParse (tempReader.ReadLine (), out double left)) Canvas.SetLeft (circle, left);
               if (double.TryParse (tempReader.ReadLine (), out double top)) Canvas.SetTop (circle, top);
               ScribblyRegion.Children.Add (circle);
               AddUIElement (circle);
               mTempCounter++;
            }
            input = tempReader.ReadLine ();
         }
      }

      /// <summary>This method is utilized to rewrite a Ellipse that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void TextWriteEllipse (ref string? input, ref StreamReader tempreader) {
         for (; input == "ellipse";) {
            if (int.TryParse (tempreader.ReadLine (), out int value)) mSetStrokeThickness = value;
            input = tempreader.ReadLine ();
            if (input != null && input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
            if (double.TryParse (tempreader.ReadLine (), out double eWidth) &&
               double.TryParse (tempreader.ReadLine (), out double eHeight)) {
               Ellipse ellipse = new () {
                  Width = eWidth, Height = eHeight, Stroke = mSetStrokeColour,
                  StrokeThickness = mSetStrokeThickness
               };
               if (double.TryParse (tempreader.ReadLine (), out double left)) Canvas.SetLeft (ellipse, left);
               if (double.TryParse (tempreader.ReadLine (), out double top)) Canvas.SetTop (ellipse, top);
               ScribblyRegion.Children.Add (ellipse);
               AddUIElement (ellipse);
               mTempCounter++;
            }
            input = tempreader.ReadLine ();
         }
      }
      #endregion

      #region To Open .bin format and  BinaryWrite Methods for Multi-Scribble and all Shapes

      /// <summary>This method is utilized to rewrite a Multi-Scribble that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="reader">It has a reference to the current line in the file.</param>
      private void BinaryWriteMultiScribble (ref string input, ref BinaryReader reader) {
         BinaryFileFormatChecker (ref input, ref reader);
         string[] firstPointArr, secondPointArr;
         try {
            for (; ; ) {
               firstPointArr = input.Split (",");
               Line line = new () {
                  X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1]),
                  Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
               };
               if ((input = reader.ReadString ()) == "scribble") {
                  BinaryFileFormatChecker (ref input, ref reader);
                  firstPointArr = input.Split (",");
                  line = new () {
                     X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1])
                  };
                  input = reader.ReadString ();
               }
               if (input == "sline" || input == "cline" || input == "rect") break;
               secondPointArr = input.Split (",");
               line.X2 = double.Parse (secondPointArr[0]);
               line.Y2 = double.Parse (secondPointArr[1]);
               ScribblyRegion.Children.Add (line);
               mUIElements.Add (line);
            }
         } catch (Exception ex) {
            MessageBox.Show ($"{ex}");
            input = "";
            return;
         }
      }

      /// <summary>To eliminate the error values (or lines in .bin file) in multiscribble.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void BinaryFileFormatChecker (ref string input, ref BinaryReader tempReader) {
         if (int.TryParse (tempReader.ReadString (), out int value)) mSetStrokeThickness = value;
         if ((input = tempReader.ReadString ()) != null && input[0] == '#')
            mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
         while (true)
            if ((input = tempReader.ReadString ()) == (_ = tempReader.ReadString ())) break;
         mTempCounter++;
      }

      /// <summary>This method is utilized to rewrite a Single Line that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void BinaryWriteSingleLine (ref string input, ref BinaryReader tempReader) {
         string firstPoint, secondPoint;
         string[] firstPointArr, secondPointArr;
         try {
            for (; input == "sline";) {
               if (int.TryParse (tempReader.ReadString (), out int value)) mSetStrokeThickness = value;
               if ((input = tempReader.ReadString ()) != null && input[0] == '#')
                  mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
               if ((firstPoint = tempReader.ReadString ()) != (secondPoint = tempReader.ReadString ())) {
                  firstPointArr = firstPoint.Split (",");
                  secondPointArr = secondPoint.Split (",");
                  Line line = new () {
                     X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1]),
                     X2 = double.Parse (secondPointArr[0]), Y2 = double.Parse (secondPointArr[1]),
                     Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
                  };
                  ScribblyRegion.Children.Add (line);
                  AddUIElement (line);
                  mTempCounter++;
               }
               input = tempReader.ReadString ();
            }
         } catch {
            input = "";
            return;
         }
      }

      /// <summary>This method is utilized to rewrite a Connected Line that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void BinaryWriteConnectedLine (ref string input, ref BinaryReader tempReader) {
         string firstPoint, secondPoint;
         string[] firstPointArr, secondPointArr;
         try {
            for (; input == "cline";) {
               if (int.TryParse (tempReader.ReadString (), out int value)) mSetStrokeThickness = value;
               if ((input = tempReader.ReadString ()) != null && input[0] == '#')
                  mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
               if ((firstPoint = tempReader.ReadString ()) != (secondPoint = tempReader.ReadString ())) {
                  firstPointArr = firstPoint.Split (",");
                  secondPointArr = secondPoint.Split (",");
                  Line line = new () {
                     X1 = double.Parse (firstPointArr[0]), Y1 = double.Parse (firstPointArr[1]),
                     X2 = double.Parse (secondPointArr[0]), Y2 = double.Parse (secondPointArr[1]),
                     Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
                  };
                  ScribblyRegion.Children.Add (line);
                  AddUIElement (line);
                  mTempCounter++;
               }
               input = tempReader.ReadString ();
            }
         } catch {
            input = "";
            return;
         }
      }

      /// <summary>This method is utilized to rewrite a Rectangle that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void BinaryWriteRectangle (ref string input, ref BinaryReader tempReader) {
         string firstPoint, secondPoint;
         string[] firstPointArr, secondPointArr;
         double x1, x2, y1, y2;
         try {
            for (; input == "rect";) {
               if (int.TryParse (tempReader.ReadString (), out int value)) mSetStrokeThickness = value;
               if ((input = tempReader.ReadString ()) != null && input[0] == '#')
                  mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
               if ((firstPoint = tempReader.ReadString ()) != (secondPoint = tempReader.ReadString ())) {
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
                  ScribblyRegion.Children.Add (rectangle);
                  AddUIElement (rectangle);
                  mTempCounter++;
               }
               input = tempReader.ReadString ();
            }
         } catch {
            input = "";
            return;
         }
      }

      /// <summary>This method is utilized to rewrite a Circle that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void BinaryWriteCircle (ref string input, ref BinaryReader tempReader) {
         try {
            for (; input == "circle";) {
               if (int.TryParse (tempReader.ReadString (), out int value)) mSetStrokeThickness = value;
               input = tempReader.ReadString ();
               if (input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
               if (double.TryParse (tempReader.ReadString (), out double diameter)) {
                  Ellipse circle = new () {
                     Width = diameter, Height = diameter
                  , Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
                  };
                  Canvas.SetLeft (circle, double.Parse (tempReader.ReadString ()));
                  Canvas.SetTop (circle, double.Parse (tempReader.ReadString ()));
                  ScribblyRegion.Children.Add (circle);
                  AddUIElement (circle);
                  mTempCounter++;
               }
               input = tempReader.ReadString ();
            }
         } catch {
            input = "";
            return;
         }
      }

      /// <summary>This method is utilized to rewrite a Ellipse that is present in a binary format file.</summary>
      /// <param name="input">It has the key ( or string, let say scribble) to start the method.
      /// Eventually to stop the switch case statement.</param>
      /// <param name="tempReader">It has a reference to the current line in the file.</param>
      private void BinaryWriteEllipse (ref string input, ref BinaryReader tempreader) {
         try {
            for (; input == "ellipse";) {
               if (int.TryParse (tempreader.ReadString (), out int value)) mSetStrokeThickness = value;
               input = tempreader.ReadString ();
               if (input[0] == '#') mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (input)!;
               Ellipse ellipse = new () {
                  Width = double.Parse (tempreader.ReadString ()),
                  Height = double.Parse (tempreader.ReadString ()), Stroke = mSetStrokeColour
               , StrokeThickness = mSetStrokeThickness
               };
               Canvas.SetLeft (ellipse, double.Parse (tempreader.ReadString ()));
               Canvas.SetTop (ellipse, double.Parse (tempreader.ReadString ()));
               ScribblyRegion.Children.Add (ellipse);
               AddUIElement (ellipse);
               mTempCounter++;
               input = tempreader.ReadString ();
            }
         } catch {
            input = "";
            return;
         }
      }

      #endregion

      #region SAVE and SAVE AS CLICK EVENTS -------------------------
      private void Save_Click (object sender, RoutedEventArgs e) => CreateDotTextFile ();

      private void SaveAsTxt_Click (object sender, RoutedEventArgs e) => CreateDotTextFile ();

      private void SaveAsBin_Click (object sender, RoutedEventArgs e) => CreateDotBinFile ();

      #endregion

      #region FILE SAVE METHODS FOR .txt and .bin -------------------

      /// <summary>To save file as Text Format (.txt).</summary>
      private void CreateDotTextFile () {
         mUIElements.Clear ();
         mSaveFile.FileName = "ScribblePad";
         mSaveFile.DefaultExt = ".txt";
         mSaveFile.Filter = "Text file (.txt)| .txt ;| Binary file (.bin) | .bin";
         CurrentLocation.Text = mSaveFile.FileName;

         if (mSaveFile.ShowDialog () == true) {
            if (File.Exists (mSaveFile.FileName)) {
               using FileStream existStream = new (mSaveFile.FileName, FileMode.Append);
               using StreamWriter writer = new (existStream);
               if (mScribbleProperties.Count != 0)
                  foreach (var a in mScribbleProperties) writer.WriteLine (a);
               if (mShapeProperties.Count != 0)
                  foreach (var a in mShapeProperties) writer.WriteLine (a);
            } else {
               using FileStream stream = new (mSaveFile.FileName, FileMode.Create);
               using StreamWriter writer = new (stream);
               if (!mWriteTextThemeOnce) {
                  writer.WriteLine (ScribblyRegion.Background);
                  writer.WriteLine (Changer.Fill);
                  mWriteTextThemeOnce = true;
               }
               if (mScribbleProperties.Count != 0)
                  foreach (var a in mScribbleProperties) writer.WriteLine (a);
               if (mShapeProperties.Count != 0)
                  foreach (var a in mShapeProperties) writer.WriteLine (a);
            }
            Title = Path.GetFileNameWithoutExtension (mSaveFile.FileName);
            mIsFileSaved = true;
         }
      }

      /// <summary>To save file as Binary Format (.bin).</summary>
      private void CreateDotBinFile () {
         mSaveFile.FileName = "ScribblePad";
         mSaveFile.Filter = "Text file (.txt) | .txt;| Binary file (.bin) | .bin";
         mSaveFile.FilterIndex = 2;
         if (mSaveFile.ShowDialog () == true) {
            if (File.Exists (mSaveFile.FileName)) {
               using FileStream stream = new (mSaveFile.FileName, FileMode.Append);
               using BinaryWriter writer = new (stream);
               if (mScribbleProperties.Count != 0)
                  foreach (var a in mScribbleProperties) writer.Write ($"{a}");
               if (mShapeProperties.Count != 0)
                  foreach (var a in mShapeProperties) writer.Write ($"{a}");
            } else {
               using FileStream stream = new (mSaveFile.FileName, FileMode.Create);
               using BinaryWriter writer = new (stream);
               if (!mWriteBinThemeOnce) {
                  writer.Write ($"{ScribblyRegion.Background}");
                  writer.Write ($"{Changer.Fill}");
                  mWriteBinThemeOnce = true;
               }
               if (mScribbleProperties.Count != 0)
                  foreach (object p in mScribbleProperties) writer.Write ($"{p}");
               if (mShapeProperties.Count != 0)
                  foreach (object p in mShapeProperties) writer.Write ($"{p}");
            }
            mIsFileSaved = true;
            Title = Path.GetFileNameWithoutExtension (mSaveFile.FileName);
         }
      }
      #endregion

      #region SAVE AND SAVE AS BUTTON DISABLE EVENT -----------------

      private void MenuItem_GotFocus (object sender, RoutedEventArgs e) {
         if (mShapeProperties.Count == 0 && mScribbleProperties.Count == 0) {
            SaveMenu.IsEnabled = false;
            SaveAsMenu.IsEnabled = false;
         } else {
            SaveMenu.IsEnabled = true;
            SaveAsMenu.IsEnabled = true;
         }
      }

      #endregion

      #endregion

      #region FILE MENU EXIT EVENT ----------------------------------
      private void FileMenuExit_Click (object sender, RoutedEventArgs e) {
         if (!mIsFileSaved && mScribbleProperties.Count == 0 && mShapeProperties.Count == 0) {
            MessageBoxResult result = MessageBox.Show ("Do you want to close the application?", "Close", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes) Close ();
         } else if (!mIsFileSaved && (mMultiScribble || mSingleLine || mConnectedLine || mRectangle || mCircle || mEllipse)) {
            mResult = MessageBox.Show ("Do you want to save this file ?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (mResult == MessageBoxResult.Yes) {
               CreateDotTextFile ();
               Close ();
            } else if (mResult == MessageBoxResult.No) Close ();
         }
      }

      private void Window_Closing (object sender, CancelEventArgs e) {
         if (!mIsFileSaved && mSingleScribble) e.Cancel = false;
         else if (!mIsFileSaved && mScribbleProperties.Count == 0 && mShapeProperties.Count == 0) {
            MessageBoxResult result = MessageBox.Show ("Do you want to close the application?", "Close", MessageBoxButton.YesNo, MessageBoxImage.Information);
            _ = result == MessageBoxResult.Yes ? e.Cancel = false : e.Cancel = true;
         } else if (!mIsFileSaved && (mSingleScribble || mMultiScribble || mMagicLine || mSingleLine
            || mConnectedLine || mRectangle || mCircle || mEllipse)) {
            mResult = MessageBox.Show ("Do you want to save this file?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (mResult == MessageBoxResult.Yes) CreateDotTextFile ();
            e.Cancel = false;
         } else if (mIsFileSaved && (mSingleScribble || mMultiScribble || mMagicLine || mSingleLine
            || mConnectedLine || mRectangle || mCircle || mEllipse))
            e.Cancel = false;
         else if (mIsFileSaved && (!mSingleScribble || !mMultiScribble || !mMagicLine || !mSingleLine
            || !mConnectedLine || !mRectangle || !mCircle || !mEllipse))
            e.Cancel = false;
      }
      #endregion

      #region EDIT CLICK EVENTS -------------------------------------
      private void SingleScribble_Click (object sender, RoutedEventArgs e) {
         mSingleScribble = true;
         mMultiScribble = mMagicLine = mSingleLine = mConnectedLine = mRectangle = mCircle = mEllipse = false;
         CurrentOperation.Text = "Single Scribble";
      }

      private void MultiScribble_Click (object sender, RoutedEventArgs e) {
         mMultiScribble = true;
         mSingleScribble = mMagicLine = mSingleLine = mConnectedLine = mRectangle = mCircle = mEllipse = false;
         CurrentOperation.Text = "Multi Scribble";
         mScribbleProperties.Clear ();
      }

      private void MagicLine_Click (object sender, RoutedEventArgs e) {
         mMagicLine = true;
         mIsDrawing = mSingleScribble = mMultiScribble = mSingleLine = mConnectedLine =
            mRectangle = mCircle = mEllipse = false;
         CurrentOperation.Text = "Magic Line";
      }

      private void SingleLine_Click (object sender, RoutedEventArgs e) {
         mSingleLine = true;
         mIsDrawing = mMultiScribble = mMagicLine = mSingleScribble = mConnectedLine = mRectangle = mCircle = mEllipse = false;
         CurrentOperation.Text = "Single Line";
      }

      private void ConnectedLine_Click (object sender, RoutedEventArgs e) {
         mConnectedLine = true;
         EscapeButton.IsEnabled = true;
         mIsDrawing = mSingleScribble = mMultiScribble = mMagicLine = mSingleLine = mRectangle = mCircle = mEllipse = false;
         CurrentOperation.Text = "Connected Lines";
         mResetCline = 1;

      }

      private void Escape_Click (object sender, RoutedEventArgs e) {
         mConnectedLine = false;
         SetStatus.Foreground = Brushes.Red;
         CurrentOperation.Text = "None";
         SetStatus.Text = "Inactive";
         mResetCline = 1;
      }

      private void Rectangle_Click (object sender, RoutedEventArgs e) {
         mRectangle = true;
         mIsDrawing = mSingleScribble = mMultiScribble = mMagicLine = mSingleLine = mConnectedLine = mCircle = mEllipse = false;
         CurrentOperation.Text = "Rectangle";
      }

      private void Circle_Click (object sender, RoutedEventArgs e) {
         CircleSetter ();
         mCircleDiameter = 20;
      }

      private void Circle_KeyDown (object sender, KeyEventArgs e) {
         if (e.Key == Key.Enter) {
            CircleSetter ();
            if (double.TryParse (Circle.Text, out double diameter)) mCircleDiameter = diameter;
            else {
               MessageBox.Show ("Invalid Format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               Circle.Text = "20";
            }
         }
      }

      /// <summary>To ensure the circle operation in the scribblypad rather than other operations.</summary>
      private void CircleSetter () {
         mCircle = true;
         mSingleScribble = mMultiScribble = mMagicLine = mSingleLine = mConnectedLine = mRectangle = mEllipse = false;
         CurrentOperation.Text = "Circle";
      }

      private void Ellipse_Click (object sender, RoutedEventArgs e) {
         EllipseSetter ();
         mEllipseWidth = 100;
         mEllipseHeight = 50;
      }

      private void Ellipse_KeyDown (object sender, KeyEventArgs e) {
         if (e.Key == Key.Enter) {
            EllipseSetter ();
            if (double.TryParse (EWidth.Text, out double width)) mEllipseWidth = width;
            else {
               MessageBox.Show ("Invalid Format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               EWidth.Text = "100";
            }
            if (double.TryParse (EHeight.Text, out double height)) mEllipseHeight = height;
            else {
               MessageBox.Show ("Invaild Format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               EHeight.Text = "50";
            }
         }
      }

      /// <summary>To ensure the ellipse operation in the scribblypad rather than other operations.</summary>
      private void EllipseSetter () {
         mEllipse = true;
         mSingleScribble = mMultiScribble = mMagicLine = mSingleLine = mConnectedLine = mRectangle = mCircle = false;
         CurrentOperation.Text = "Ellipse";
      }

      #endregion

      #region COMMAND BINDINGS for UNDO, REDO and SAVE --------------
      private void Undo_CanExecute (object sender, CanExecuteRoutedEventArgs e) {
         if (mUndoUIElements.Count > 0)
            e.CanExecute = true;
         else e.CanExecute = false;
      }

      private void Undo_Executed (object sender, ExecutedRoutedEventArgs e) {
         if (mIsFileOpened && mUndoCounter == mTempCounter) {
            while (mRedoCounter < mTempCounter)
               UndoMethod ();
         } else UndoMethod ();
      }

      /// <summary>To Undo the UI elements from the scribblyregion on the scribblypad.</summary>
      private void UndoMethod () {
         List<UIElement> uIElements = mUndoUIElements[--mUndoCounter];
         mRedoUIElements.Add (uIElements);
         mRedoCounter++;
         mUndoUIElements.RemoveAt (mUndoCounter);
         foreach (var a in uIElements)
            ScribblyRegion.Children.Remove (a);
      }

      private void Redo_CanExecute (object sender, CanExecuteRoutedEventArgs e) {
         if (mRedoUIElements.Count > 0)
            e.CanExecute = true;
         else e.CanExecute = false;
      }

      private void Redo_Executed (object sender, ExecutedRoutedEventArgs e) {
         if (mIsFileOpened && mRedoCounter == mTempCounter) {
            while (mUndoCounter < mTempCounter)
               RedoMethod ();
         } else RedoMethod ();
      }

      /// <summary>To Redo the UI elements from the scribblyregion on the scribblypad.</summary>
      private void RedoMethod () {
         List<UIElement> uIElements = mRedoUIElements[--mRedoCounter];
         mUndoUIElements.Add (uIElements);
         mUndoCounter++;
         mRedoUIElements.RemoveAt (mRedoCounter);
         try {
            foreach (var a in uIElements)
               ScribblyRegion.Children.Add (a);
         } catch {
            return;
         }
      }

      private void Save_CanExecute (object sender, CanExecuteRoutedEventArgs e) {
         if (mScribbleProperties.Count > 0 || mShapeProperties.Count > 0)
            e.CanExecute = true;
      }

      private void Save_Executed (object sender, ExecutedRoutedEventArgs e)
         => CreateDotTextFile ();
      #endregion

      #region SET STROKE COLOURS ------------------------------------
      private void IndianRed_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
         => mSetStrokeColour = Brushes.IndianRed;

      private void ForestGreen_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
         => mSetStrokeColour = Brushes.ForestGreen;

      private void Yellow_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
         => mSetStrokeColour = Brushes.Yellow;

      private void LightSkyBlue_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
         => mSetStrokeColour = Brushes.LightSkyBlue;

      private void RosyBrown_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
         => mSetStrokeColour = Brushes.RosyBrown;

      private void BlueVoilet_LeftButtonButtonDown (object sender, MouseButtonEventArgs e)
         => mSetStrokeColour = Brushes.BlueViolet;

      private void Pink_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
         => mSetStrokeColour = Brushes.Pink;

      private void Orange_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
         => mSetStrokeColour = Brushes.Orange;

      private void WhiteSmoke_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
        => mSetStrokeColour = Brushes.WhiteSmoke;
      #endregion

      #region MENUBAR GOT FOCUS EVENT -------------------------------
      private void MenuBar_GotFocus (object sender, RoutedEventArgs e) {
         if (mIntialThemeChange) ThemeChange ();
         if (!mConnectedLine) EscapeButton.IsEnabled = false;
      }
      #endregion

      #region SET STROKE THICKNESS ----------------------------------
      private void SetThickness_KeyDown (object sender, KeyEventArgs e) {
         if (e.Key == Key.Enter) {
            bool result = int.TryParse (TextToSliderValue.Text, out int value);
            if (result && value >= 1 && value <= 10)
               TextToSliderValue.Text = $"{value}";
            else if (result && value < 1) TextToSliderValue.Text = "1";
            else if (result && value > 10) TextToSliderValue.Text = "10";
            else {
               MessageBox.Show ("Invalid Format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               TextToSliderValue.Text = "1";
            }
         } else {
            MessageBox.Show ("Invalid Format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TextToSliderValue.Text = "1";
         }
      }
      #endregion

      #region CLEAR METHOD ------------------------------------------
      private void Clear_Click (object sender, RoutedEventArgs e) => Clear ();

      /// <summary>To clear all the objects (or UI elements) in ScribblyRegion on the ScribblyPad.</summary>
      private void Clear () {
         ScribblyRegion.Children.Clear ();
         mScribbleProperties.Clear ();
         mShapeProperties.Clear ();
         mUndoUIElements.Clear ();
         mRedoUIElements.Clear ();
         mResetCline = 1;
         mUndoCounter = 0;
         mRedoCounter = 0;
      }
      #endregion

      #region THEME CHANGE METHOD -----------------------------------
      /// <summary>To alter the ScribblyPad's theme at the beginning.</summary>
      private void ThemeChange () {
         if (MessageBox.Show ("Do you want to change Dark theme to Light theme", "Theme",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
            ScribblyRegion.Background = Brushes.White;
            Changer.Fill = Brushes.Black;
            mSetStrokeColour = Brushes.Black;
            Changer.ToolTip = "Black";
         }
         mIntialThemeChange = false;
      }
      #endregion

      #region PRIVATE FIELDS ----------------------------------------
      Line mLine = new ();
      Point mPoint = new ();
      bool mIsDrawing, mLeftButtonPressed, mMagicLine, mSingleScribble = true, mMultiScribble,
         mMouseLeave = true, mSingleLine, mConnectedLine, mRectangle, mIntialThemeChange = true,
         mIsFileSaved = false, mIsFileOpened, mWriteTextThemeOnce, mWriteBinThemeOnce, mCircle,
         mEllipse;
      Brush mSetStrokeColour = Brushes.White;
      readonly Polyline mPolyLine = new ();
      PointCollection mPointCollection = new ();
      SaveFileDialog mSaveFile = new ();
      List<List<UIElement>> mUndoUIElements = new (), mRedoUIElements = new ();
      List<UIElement> mUIElements = new ();
      ArrayList mScribbleProperties = new (), mShapeProperties = new ();
      int mSetStrokeThickness = 1, mResetCline = 1, mCounter, mUndoCounter,
         mRedoCounter, mTempCounter;
      double mCircleDiameter, mEllipseWidth, mEllipseHeight;
      readonly string[] mElements = { "scribble", "sline", "cline", "rect", "circle", "ellipse" };
      MessageBoxResult mResult;
      #endregion
   }
}