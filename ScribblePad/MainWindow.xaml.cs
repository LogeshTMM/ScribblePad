using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;
using static ScribblyPadLibrary.Fields;
using static ShapeLibrary.BinaryWriter;
using static ShapeLibrary.TextWriter;

namespace CadKit {
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
            CadKitRegion.Children.Clear ();
            mScribbleProperties.Clear ();
            ScribbleInfo ();
         } else if (mMultiScribble && sender is Canvas) {
            ScribbleInfo ();
            if (mIsFileSaved) mIsFileSaved = false;
         }

         if (mRectangle) {
            if (mIsFileSaved) mIsFileSaved = false;
            mShapeProperties.Add ("rect");
            mShapeProperties.Add (mSetStrokeThickness);
            mShapeProperties.Add (mSetStrokeColour);
            mPoint = e.GetPosition (CadKitRegion);
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
               mPoint = e.GetPosition (CadKitRegion);
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
            mPoint = e.GetPosition (CadKitRegion);
         } else if (mMagicLine == true && sender is Canvas) {
            mScribbleProperties.Clear ();
            CadKitRegion.Children.Clear ();
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
            mPoint = e.GetPosition (CadKitRegion);
            mLine.X2 = mPoint.X;
            mLine.Y2 = mPoint.Y;
            mScribbleProperties.Add (mPoint);
            mLine.Stroke = mSetStrokeColour;
            mLine.StrokeThickness = mSetStrokeThickness;
            CadKitRegion.Children.Add (mLine);
            if (mIsFileSaved) mIsFileSaved = false;
            mUIElements.Add (mLine); // scribble have many line object in one scribble so we add list of uielement
                                     // mUIElement and leftbutton up event add the whole number of mLine object 
                                     // let say minimun 250 plus in list of list of uielement mUndoUIElement.
         } else if (mLeftButtonPressed == true && mMagicLine && sender is Canvas && !mMouseLeave) {
            mPoint = e.GetPosition (CadKitRegion);
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
         if (sender is Canvas && mMagicLine == true) CadKitRegion.Children.Add (mPolyLine);
         else if (mSingleLine && !mMouseLeave) {
            if (mTempSLine == 1) {
               mPoint = e.GetPosition (CadKitRegion);
               mLine = new () {
                  X1 = mPoint.X, Y1 = mPoint.Y,
                  Stroke = mSetStrokeColour,
                  StrokeThickness = mSetStrokeThickness
               };
               mShapeProperties.Add ("sline");
               mShapeProperties.Add (mSetStrokeThickness);
               mShapeProperties.Add (mSetStrokeColour);
               mShapeProperties.Add (mPoint);
               mTempSLine++;
            } else if (mTempSLine > 1) {
               mPoint = e.GetPosition (CadKitRegion);
               mLine.X2 = mPoint.X;
               mLine.Y2 = mPoint.Y;
               mShapeProperties.Add (mPoint);
               CadKitRegion.Children.Add (mLine);
               AddUIElement (mLine);
               mTempSLine--;
            }
         } else if (mRectangle && !mMouseLeave) {
            mPoint = e.GetPosition (CadKitRegion);
            mLine.X2 = mPoint.X;
            mLine.Y2 = mPoint.Y;
            mShapeProperties.Add (mPoint);
            if (mLine.X1 != mLine.X2 && mLine.Y1 != mLine.Y2) {
               Rectangle rect = new () {
                  Width = Math.Abs (mLine.X2 - mLine.X1), Height = Math.Abs (mLine.Y2 - mLine.Y1),
                  Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
               };
               if (mLine.X1 < mLine.X2) Canvas.SetLeft (rect, mLine.X1);
               else Canvas.SetLeft (rect, mLine.X2);
               if (mLine.Y2 < mLine.Y1) Canvas.SetTop (rect, Math.Abs (mLine.Y2));
               else Canvas.SetTop (rect, Math.Abs (mLine.Y1));
               CadKitRegion.Children.Add (rect);
               AddUIElement (rect);
            }
         } else if (mConnectedLine) {
            mPoint = e.GetPosition (CadKitRegion);
            mLine.X2 = mPoint.X;
            mLine.Y2 = mPoint.Y;
            mShapeProperties.Add (mPoint);
            mLine.Stroke = mSetStrokeColour;
            mLine.StrokeThickness = mSetStrokeThickness;
            CadKitRegion.Children.Add (mLine);
            AddUIElement (mLine);
         } else if (mCircle) {
            if (mCircleDiameter < this.Height) {
               Ellipse circle = new () {
                  Width = mCircleDiameter, Height = mCircleDiameter,
                  Stroke = mSetStrokeColour, StrokeThickness = mSetStrokeThickness
               };
               mPoint = e.GetPosition (CadKitRegion);
               if (!mMouseLeave && mPoint.Y > (mCircleDiameter / 2)) {
                  Canvas.SetLeft (circle, mPoint.X - (circle.Width / 2));
                  Canvas.SetTop (circle, mPoint.Y - (circle.Width / 2));
                  CadKitRegion.Children.Add (circle);
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
               mPoint = e.GetPosition (CadKitRegion);
               if (!mMouseLeave && mPoint.Y > (mEllipseHeight / 2)) {
                  Canvas.SetLeft (ellipse, mPoint.X - mEllipseWidth / 2);
                  Canvas.SetTop (ellipse, mPoint.Y - mEllipseHeight / 2);
                  CadKitRegion.Children.Add (ellipse);
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
         else if (!mIsFileSaved && (mScribbleProperties.Count == 0 || mShapeProperties.Count == 0)) NewFileSetter ();
      }

      private void NewFileSetter () {
         Title = "Cad Kit";
         Clear ();
         mSaveFile.InitialDirectory = CurrentLocation.Text = Environment.CurrentDirectory;
         mSingleScribble = true;
         XPosition.Text = YPosition.Text = "0";
         mMultiScribble = mMagicLine = mSingleLine = mConnectedLine = mRectangle = mCircle = mEllipse = false;
         CurrentOperation.Text = "Single Scribble";
         (mUndoCounter, mRedoCounter, mTempCounter) = (0, 0, 0);
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
                  if (point != null && point[0] == '#') {
                     Brush temp = (SolidColorBrush)new BrushConverter ().ConvertFrom (point)!, temp1 = CadKitRegion.Background;
                     _ = temp1.ToString () == temp.ToString () ? CadKitRegion.Background = temp :
                        throw new Exception ("Theme mismatch");
                  } else throw new Exception ("It's not a Scribbly Pad file.");
                  point = reader.ReadLine ();
                  if (point != null && point[0] == '#') {
                     Changer.Fill = mSetStrokeColour = (SolidColorBrush)new BrushConverter ().ConvertFrom (point)!;
                     _ = point == "#FFF5F5F5" ? Changer.ToolTip = "White" : Changer.ToolTip = "Black";
                  }
                  CadKitRegion.Children.Clear ();
                  point = reader.ReadLine ();
                  while (true) {
                     switch (point) {
                        case "scribble":
                           mDisplayUIElements = new ();
                           TextWriterScribble (ref point, ref reader);
                           if (!mDontAddTxt) {
                              mDisplayUIElements = mUIElements;
                              mUndoUIElements.Add (mUIElements);
                              mUIElements = new ();
                           }
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;
                        case "sline":
                           mDisplayUIElements = new ();
                           TextWriterSingleLine (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;

                        case "cline":
                           mDisplayUIElements = new ();
                           TextWriterConnectedLine (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;

                        case "rect":
                           mDisplayUIElements = new ();
                           TextWriterRectangle (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;

                        case "circle":
                           mDisplayUIElements = new ();
                           TextWriterCircle (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;

                        case "ellipse":
                           mDisplayUIElements = new ();
                           TextWriterEllipse (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;

                        default: break;
                     }
                     if (point == null) break; // Don't remove null because ReadString() returns null when it reached end of the file.
                  }

               } else if (fileExetension == ".bin") {
                  BinaryReader reader = new (fileStream);
                  string point = reader.ReadString ();
                  if (point != null && point[0] == '#') {
                     Brush temp = (SolidColorBrush)new BrushConverter ().ConvertFrom (point)!, temp1 = CadKitRegion.Background;
                     _ = temp1.ToString () == temp.ToString () ? CadKitRegion.Background = temp :
                         throw new Exception ("Theme mismatch");
                  } else throw new Exception ("It's not a Scribbly Pad file");
                  point = reader.ReadString ();
                  if (point != null && point[0] == '#') {
                     Changer.Fill = (SolidColorBrush)new BrushConverter ().ConvertFrom (point)!;
                     _ = point == "#FFF5F5F5" ? Changer.ToolTip = "White" : Changer.ToolTip = "Black";
                  }
                  CadKitRegion.Children.Clear ();
                  point = reader.ReadString ();
                  while (true) {
                     switch (point) {
                        case "scribble":
                           mDisplayUIElements = new ();
                           BinaryWriterScribble (ref point, ref reader);
                           if (!mDontAddBin) {
                              mDisplayUIElements = mUIElements;
                              mUndoUIElements.Add (mUIElements);
                              mUIElements = new ();
                           }
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;
                        case "sline":
                           mDisplayUIElements = new ();
                           BinaryWriterSingleLine (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;
                        case "cline":
                           mDisplayUIElements = new ();
                           BinaryWriterConnectedLine (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;
                        case "rect":
                           mDisplayUIElements = new ();
                           BinaryWriteRectangle (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;
                        case "circle":
                           mDisplayUIElements = new ();
                           BinaryWriterCircle (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
                           break;
                        case "ellipse":
                           mDisplayUIElements = new ();
                           BinaryWriterEllipse (ref point, ref reader);
                           if (mDisplayUIElements.Count != 0) DisplayUIElements ();
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

      private void DisplayUIElements () {
         for (int i = 0; i < mDisplayUIElements.Count; i++)
            CadKitRegion.Children.Add (mDisplayUIElements[i]);
      }
      #endregion

      #region SAVE and SAVE AS Methods ------------------------------

      #region SAVE and SAVE AS CLICK EVENTS -------------------------
      private void Save_Click (object sender, RoutedEventArgs e) => CreateDotTextFile ();

      private void SaveAsTxt_Click (object sender, RoutedEventArgs e) => CreateDotTextFile ();

      private void SaveAsBin_Click (object sender, RoutedEventArgs e) => CreateDotBinFile ();

      #endregion

      #region FILE SAVE METHODS FOR .txt and .bin -------------------

      /// <summary>To save file as Text Format (.txt).</summary>
      private void CreateDotTextFile () {
         mUIElements.Clear ();
         mSaveFile.FileName = "CadKit";
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
                  writer.WriteLine (CadKitRegion.Background);
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
         mSaveFile.FileName = "CadKit";
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
                  writer.Write ($"{CadKitRegion.Background}");
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
         if (e.Key == Key.Enter || e.Key == Key.Tab) {
            CircleSetter ();
            if (double.TryParse (Circle.Text, out double diameter)) {
               if (diameter < 0) {
                  MessageBox.Show ("Negative value is not accepted, so set to default value",
                     "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                  CircleToolBar.Text = "20";
                  mCircleDiameter = 20;
               } else mCircleDiameter = diameter;
            } else {
               MessageBox.Show ("Invalid Format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               Circle.Text = "20";
               mCircleDiameter = 20;
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
         if (e.Key == Key.Enter || e.Key == Key.Tab) {
            EllipseSetter ();
            if (double.TryParse (EWidth.Text, out double width))
               if (width < 0) {
                  MessageBox.Show ("Negative value is not accepted, so set to default value",
                     "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                  EWidth.Text = "100";
                  mEllipseWidth = 100;
               } else mEllipseWidth = width;
            else {
               MessageBox.Show ("Invalid Format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
               EWidth.Text = "100";
            }
            if (double.TryParse (EHeight.Text, out double height))
               if (height < 0) {
                  MessageBox.Show ("Negative value is not accepted, so set to default value",
                     "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                  EHeight.Text = "50";
                  mEllipseHeight = 50;
               } else mEllipseHeight = height;
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
         bool loopCutter = true; int temp = mTempCounter;
         if (mIsFileOpened && mUndoCounter == mTempCounter) {
            while (loopCutter && mTempCounter-- != 0)
               UndoMethod (ref loopCutter, temp);
         } else UndoMethod (ref loopCutter, temp);
      }

      /// <summary>To Undo the UI elements from the scribblyregion on the scribblypad.</summary>
      private void UndoMethod (ref bool loopCutter, int value) {
         List<UIElement> uIElements = mUndoUIElements[--mUndoCounter];
         mRedoUIElements.Add (uIElements);
         mRedoCounter++;
         mUndoUIElements.RemoveAt (mUndoCounter);
         foreach (var a in uIElements)
            CadKitRegion.Children.Remove (a);
         if (mTempCounter == 0) {
            mTempCounter = value;
            loopCutter = false;
         }
      }

      private void Redo_CanExecute (object sender, CanExecuteRoutedEventArgs e) {
         if (mRedoUIElements.Count > 0)
            e.CanExecute = true;
         else e.CanExecute = false;
      }

      private void Redo_Executed (object sender, ExecutedRoutedEventArgs e) {
         bool loopCutter = true; int temp = mTempCounter;
         if (mIsFileOpened && mRedoCounter == mTempCounter) {
            while (loopCutter && mTempCounter-- != 0)
               RedoMethod (ref loopCutter, temp);
         } else RedoMethod (ref loopCutter, temp);
      }

      /// <summary>To Redo the UI elements from the scribblyregion on the scribblypad.</summary>
      private void RedoMethod (ref bool loopCutter, int value) {
         List<UIElement> uIElements = mRedoUIElements[--mRedoCounter];
         mUndoUIElements.Add (uIElements);
         mUndoCounter++;
         mRedoUIElements.RemoveAt (mRedoCounter);
         try {
            foreach (var a in uIElements)
               CadKitRegion.Children.Add (a);
            if (mTempCounter == 0) {
               mTempCounter = value;
               loopCutter = false;
            }
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
         CadKitRegion.Children.Clear ();
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
            CadKitRegion.Background = Brushes.White;
            Changer.Fill = Brushes.Black;
            mSetStrokeColour = Brushes.Black;
            Changer.ToolTip = "Black";
         }
         mIntialThemeChange = false;
      }
      #endregion
   }
}