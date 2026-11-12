' ################################################################################
' #                             EMBER MEDIA MANAGER                              #
' ################################################################################
' ################################################################################
' # This file is part of Ember Media Manager.                                    #
' #                                                                              #
' # Ember Media Manager is free software: you can redistribute it and/or modify  #
' # it under the terms of the GNU General Public License as published by         #
' # the Free Software Foundation, either version 3 of the License, or            #
' # (at your option) any later version.                                          #
' #                                                                              #
' # Ember Media Manager is distributed in the hope that it will be useful,       #
' # but WITHOUT ANY WARRANTY; without even the implied warranty of               #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ################################################################################
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Windows.Forms
Imports System.IO

Imports EmberAPI
Imports System.Drawing
Imports UnitTests

Namespace EmberTests


    <TestClass()> Public Class Test_clsAPIImageUtils

        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_AddMissingStamp_NothingParameter()
            'Arrange

            'Act
            Dim result As Images = ImageUtils.AddMissingStamp(Nothing)
            'Assert
            Assert.IsTrue(result Is Nothing, "Nothing parameter")
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_AddMissingStamp()
            'Arrange
            Dim userResponse As Windows.Forms.DialogResult = Nothing
            'EmberAPI.Images implementiert kein IDisposable - deshalb wird explizit ueber
            'Clear() aufgeraeumt, damit die GDI-Handles freigegeben werden.
            Dim img As New EmberAPI.Images()
            Try
                img.UpdateMSfromImg(My.Resources.TestPattern)

                'Act
                'AddMissingStamp liefert dieselbe Instanz zurueck, die uebergeben wurde.
                Dim result As EmberAPI.Images = ImageUtils.AddMissingStamp(img)
                Using dialog As ImageFeedback = New ImageFeedback()
                    dialog.LoadInfo(result.Image, "Is there a Missing stamp across the top left?")
                    userResponse = dialog.ShowDialog()
                End Using
            Finally
                img.Clear()
            End Try
            'Assert
            Assert.IsTrue(userResponse = Windows.Forms.DialogResult.Yes, "User disagreed")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_Grayscale_NothingParameter()
            'Arrange

            'Act
            Dim result As Images = ImageUtils.GrayScale(Nothing)
            'Assert
            Assert.IsTrue(result Is Nothing, "Nothing parameter")
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_GrayScale()
            'Arrange
            Dim userResponse As System.Windows.Forms.DialogResult = DialogResult.None
            'EmberAPI.Images implementiert kein IDisposable - deshalb wird explizit ueber
            'Clear() aufgeraeumt, damit die GDI-Handles freigegeben werden.
            Dim img As New EmberAPI.Images()
            Try
                img.UpdateMSfromImg(My.Resources.TestPattern)

                'Act
                'GrayScale veraendert die uebergebene Instanz und liefert sie zurueck.
                Dim result As EmberAPI.Images = ImageUtils.GrayScale(img)
                Using dialog As ImageFeedback = New ImageFeedback()
                    dialog.LoadInfo(result.Image, "Is this image in grayscale?")
                    userResponse = dialog.ShowDialog()
                End Using
            Finally
                img.Clear()
            End Try
            'Assert
            Assert.IsTrue(userResponse = Windows.Forms.DialogResult.Yes, "User disagreed")

        End Sub
        'Hinweis: ImageUtils.DrawGradEllipse existiert in EmberAPI nicht mehr und hat keinen
        'Ersatz. Die beiden zugehoerigen Tests wurden deshalb entfernt.
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_NothingImage()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = Nothing

            Dim maxWidth = 200
            Dim maxHeight = 200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)
            success = image Is Nothing

            'Assert
            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_ZeroWidth()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern     ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 0
            Dim maxHeight = 200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)

            'Assert
            Dim widthOK = image.Width = My.Resources.TestPattern.Width
            Dim heightOK = image.Height = My.Resources.TestPattern.Height
            success = widthOK And heightOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_ZeroHeight()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern     ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 200
            Dim maxHeight = 0

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)

            'Assert
            Dim widthOK = image.Width = My.Resources.TestPattern.Width
            Dim heightOK = image.Height = My.Resources.TestPattern.Height
            success = widthOK And heightOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_NegativeWidth()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern  ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = -200
            Dim maxHeight = 200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)

            'Assert
            Dim widthOK = image.Width = My.Resources.TestPattern.Width
            Dim heightOK = image.Height = My.Resources.TestPattern.Height
            success = widthOK And heightOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_NegativeHeight()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern     ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 200
            Dim maxHeight = -200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)

            'Assert
            Dim widthOK = image.Width = My.Resources.TestPattern.Width
            Dim heightOK = image.Height = My.Resources.TestPattern.Height
            success = widthOK And heightOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_Shrink()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern     ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 200
            Dim maxHeight = 200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with no padding?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using

            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
       <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_Grow()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern_Med     ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 800
            Dim maxHeight = 800

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with no padding?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using
            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_Shrink_WithPadding()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern  ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 200
            Dim maxHeight = 200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight, True)

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with Black padding on the top and bottom?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using

            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing
            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
       <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_Grow_WithPadding()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern_Med   ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 800
            Dim maxHeight = 800

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight, True)

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with Black padding on top and bottom?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using

            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_Shrink_WithGreenPaddingTopBottom()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern  ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 200
            Dim maxHeight = 200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight, True, Color.Green.ToArgb())

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with Green padding on top and bottom?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using

            success = widthOK And heightOK And scaleOK And visualOK

            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_Shrink_WithGreenPaddingSides()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern  ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 400
            Dim maxHeight = 200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight, True, Color.Green.ToArgb())

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with bottom portion missing?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using

            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_ShrinkVert_WithGreenPaddingSides()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern_Vert  ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 400
            Dim maxHeight = 200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight, True, Color.Green.ToArgb())

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with Green padding on the sides?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using

            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
         <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_Grow_WithGreenPaddingTopBottom()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern_Med  ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 800
            Dim maxHeight = 800

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight, True, Color.Green.ToArgb())

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with Green padding on top and bottom?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using

            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_Grow_WithGreenPaddingSides()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern_Med     ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 800
            Dim maxHeight = 400

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight, True, Color.Green.ToArgb())

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with bottom portion missing?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using

            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_width_height_padding_GrowVert_WithGreenPaddingSides()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = My.Resources.TestPattern_Med_Vert    ' Cannot use "using" because of ByVal in ResizeImage

            Dim maxWidth = 800
            Dim maxHeight = 400

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight, True, Color.Green.ToArgb())

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= maxWidth
            Dim heightOK = image.Height <= maxHeight
            Dim scaleOK = (image.Width = maxWidth) OrElse (image.Height = maxHeight)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is the image close-cropped, with Green padding on the sides?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.Yes    'Yes means it looked good
            End Using

            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <UnitTest>
         <TestMethod()>
        Public Sub ImageUtils_ResizePB_NothingSourceImage()
            'Arrange
            Dim success As Boolean = False
            Dim destImage As PictureBox = New PictureBox()
            Using sourceImage As PictureBox = New PictureBox()

                Dim boxWidth = 600
                Dim boxHeight = 400

                'Act
                ImageUtils.ResizePB(destImage, sourceImage, boxHeight, boxWidth)
                success = destImage.Image Is Nothing
            End Using
            destImage.Dispose()
            destImage = Nothing
            'Assert
            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizePB_NothingSource()
            'Arrange
            Dim success As Boolean = False
            Dim destImage As PictureBox = New PictureBox()

            Dim boxWidth = 600
            Dim boxHeight = 400

            'Act
            ImageUtils.ResizePB(destImage, Nothing, boxHeight, boxWidth)
            success = destImage.Image Is Nothing
            destImage.Dispose()
            destImage = Nothing
            'Assert
            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizePB_Wide()
            'Arrange
            Dim success As Boolean = False
            Dim destImage As PictureBox = New PictureBox()
            Using sourceImage As PictureBox = New PictureBox()

                sourceImage.Image = My.Resources.TestPattern_Med
                Dim boxWidth = 600
                Dim boxHeight = 400

                'Act
                ImageUtils.ResizePB(destImage, sourceImage, boxHeight, boxWidth)

                Dim widthOK As Boolean = destImage.Width <= IIf(sourceImage.Image.Width >= boxWidth, boxWidth, sourceImage.Image.Width)
                Dim heightOK As Boolean = destImage.Height <= IIf(sourceImage.Image.Height >= boxHeight, boxHeight, sourceImage.Image.Height)
                success = widthOK And heightOK
            End Using
            destImage.Dispose()
            destImage = Nothing

            'Assert
            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizePB_Tall()
            'Arrange
            Dim success As Boolean = False
            Dim destImage As PictureBox = New PictureBox()
            Using sourceImage As PictureBox = New PictureBox()

                sourceImage.Image = My.Resources.TestPattern_Med
                Dim boxWidth = 400
                Dim boxHeight = 600

                'Act
                ImageUtils.ResizePB(destImage, sourceImage, boxHeight, boxWidth)

                'Assert
                Dim widthOK As Boolean = destImage.Width <= IIf(sourceImage.Image.Width >= boxWidth, boxWidth, sourceImage.Image.Width)
                Dim heightOK As Boolean = destImage.Height <= IIf(sourceImage.Image.Height >= boxHeight, boxHeight, sourceImage.Image.Height)
                success = widthOK And heightOK
            End Using
            destImage.Dispose()
            destImage = Nothing

            'Assert
            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizePB_Tall_Shrink()
            'Arrange
            Dim success As Boolean = False
            Dim destImage As PictureBox = New PictureBox()
            Using sourceImage As PictureBox = New PictureBox()

                sourceImage.Image = My.Resources.TestPattern_Med
                Dim boxWidth = 200
                Dim boxHeight = 400

                'Act
                ImageUtils.ResizePB(destImage, sourceImage, boxHeight, boxWidth)

                'Assert
                Dim widthOK As Boolean = destImage.Width <= IIf(sourceImage.Image.Width >= boxWidth, boxWidth, sourceImage.Image.Width)
                Dim heightOK As Boolean = destImage.Height <= IIf(sourceImage.Image.Height >= boxHeight, boxHeight, sourceImage.Image.Height)
                success = widthOK And heightOK
            End Using
            destImage.Dispose()
            destImage = Nothing

            'Assert
            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizePB_Wide_Shrink()
            'Arrange
            Dim success As Boolean = False
            Dim destImage As PictureBox = New PictureBox()
            Using  sourceImage As PictureBox = New PictureBox()

                sourceImage.Image = My.Resources.TestPattern_Med
                Dim boxWidth = 400
                Dim boxHeight = 200

                'Act
                ImageUtils.ResizePB(destImage, sourceImage, boxHeight, boxWidth)

                'Assert
                Dim widthOK As Boolean = destImage.Width <= IIf(sourceImage.Image.Width >= boxWidth, boxWidth, sourceImage.Image.Width)
                Dim heightOK As Boolean = destImage.Height <= IIf(sourceImage.Image.Height >= boxHeight, boxHeight, sourceImage.Image.Height)
                success = widthOK And heightOK
            End Using
            destImage.Dispose()
            destImage = Nothing

            'Assert
            Assert.IsTrue(success)
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_SetGlassOverlay_NothingParameter()
            'Arrange
            Dim source As PictureBox = Nothing
            'Act
            ImageUtils.SetGlassOverlay(source)

            'Assert
            Assert.IsNull(source, "Expected Nothing, got something else")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_SetGlassOverlay_NothingImage()
            'Arrange
            Dim source As PictureBox = New PictureBox()
            source.Image = Nothing
            'Act
            ImageUtils.SetGlassOverlay(source)

            'Assert
            Assert.IsNull(source.Image, "Expected Nothing, got something else")
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_SetGlassOverlay_NormalImage_Horizontal()
            'Arrange
            Dim userResponse As Windows.Forms.DialogResult = Nothing
            Using source As PictureBox = New PictureBox()
                source.Image = My.Resources.TestPattern_Med

                'Act
                ImageUtils.SetGlassOverlay(source)

                Using dialog As ImageFeedback = New ImageFeedback()
                    dialog.LoadInfo(source.Image, "Does the image have a Glass overlay?")
                    userResponse = dialog.ShowDialog()
                End Using
            End Using

            'Assert
            Assert.IsTrue(userResponse = Windows.Forms.DialogResult.Yes, "User disagreed")
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_SetGlassOverlay_NormalImage_Vertical()
            'Arrange
            Dim userResponse As Windows.Forms.DialogResult = Nothing
            Using source As PictureBox = New PictureBox()
                source.Image = My.Resources.TestPattern_Med_Vert

                'Act
                ImageUtils.SetGlassOverlay(source)

                Using dialog As ImageFeedback = New ImageFeedback()
                    dialog.LoadInfo(source.Image, "Does the image have a Glass overlay?")
                    userResponse = dialog.ShowDialog()
                End Using
            End Using

            'Assert
            Assert.IsTrue(userResponse = Windows.Forms.DialogResult.Yes, "User disagreed")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_SetOverlay_NothingImage()
            'Arrange
            Dim overlay As Image = Image.FromFile(Path.Combine(Functions.AppPath, "Images", "Defaults", "HasLanguage.png"))            'Act
            Dim result As Image = ImageUtils.SetOverlay(Nothing, 500, 500, overlay, 1)

            'Assert
            Assert.IsNull(result, "Expected Nothing, got something else")
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_SetOverlay_TopLeft()
            'Arrange
            Dim userResponse As Windows.Forms.DialogResult = Nothing
            Using img As Image = My.Resources.TestPattern, _
                overlay As Image = Image.FromFile(Path.Combine(Functions.AppPath, "Images", "Defaults", "HasLanguage.png"))

                'Act
                Using result As Image = ImageUtils.SetOverlay(img, img.Width \ 2, img.Height \ 2, overlay, 1)
                    'result.Save("TestPatternWithLanguage_Top_Left.png", System.Drawing.Imaging.ImageFormat.Png)
                    Using dialog As ImageFeedback = New ImageFeedback()
                        dialog.LoadInfo(result, "Is there a message bubble in the top-left corner?")
                        userResponse = dialog.ShowDialog()
                    End Using
                End Using
            End Using

            'Assert
            Assert.IsTrue(userResponse = Windows.Forms.DialogResult.Yes, "User disagreed")
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_SetOverlay_TopRight()
            'Arrange
            Dim userResponse As Windows.Forms.DialogResult = Nothing
            Using img As Image = My.Resources.TestPattern, _
                overlay As Image = Image.FromFile(Path.Combine(Functions.AppPath, "Images", "Defaults", "HasLanguage.png"))

                'Act
                Using result As Image = ImageUtils.SetOverlay(img, img.Width \ 2, img.Height \ 2, overlay, 2)
                    'result.Save("TestPatternWithLanguage_Top_Right.png", System.Drawing.Imaging.ImageFormat.Png)
                    Using dialog As ImageFeedback = New ImageFeedback()
                        dialog.LoadInfo(result, "Is there a message bubble in the top-right corner?")
                        userResponse = dialog.ShowDialog()
                    End Using
                End Using
            End Using
            'Assert
            Assert.IsTrue(userResponse = Windows.Forms.DialogResult.Yes, "User disagreed")
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_SetOverlay_BottomLeft()
            'Arrange
            Dim userResponse As Windows.Forms.DialogResult = Nothing
            Using img As Image = My.Resources.TestPattern, _
                overlay As Image = Image.FromFile(Path.Combine(Functions.AppPath, "Images", "Defaults", "HasLanguage.png"))

                'Act
                Using result As Image = ImageUtils.SetOverlay(img, img.Width \ 2, img.Height \ 2, overlay, 3)
                    'result.Save("TestPatternWithLanguage_Bottom_Left.png", System.Drawing.Imaging.ImageFormat.Png)
                    Using dialog As ImageFeedback = New ImageFeedback()
                        dialog.LoadInfo(result, "Is there a message bubble in the lower-left corner?")
                        userResponse = dialog.ShowDialog()
                    End Using
                End Using
            End Using

            'Assert
            Assert.IsTrue(userResponse = Windows.Forms.DialogResult.Yes, "User disagreed")
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_SetOverlay_BottomRight()
            'Arrange
            Dim userResponse As Windows.Forms.DialogResult = Nothing
            Using img As Image = My.Resources.TestPattern, _
                overlay As Image = Image.FromFile(Path.Combine(Functions.AppPath, "Images", "Defaults", "HasLanguage.png"))
                'Act
                Using result As Image = ImageUtils.SetOverlay(img, img.Width \ 2, img.Height \ 2, overlay, 4)
                    'result.Save("TestPatternWithLanguage_Bottom_Right.png", System.Drawing.Imaging.ImageFormat.Png)
                    Using dialog As ImageFeedback = New ImageFeedback()
                        dialog.LoadInfo(result, "Is there a message bubble in the lower-right corner?")
                        userResponse = dialog.ShowDialog()
                    End Using
                End Using
            End Using

            'Assert
            Assert.IsTrue(userResponse = Windows.Forms.DialogResult.Yes, "User disagreed")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_JPEGCompression()
            'Arrange
            'Act
            'Assert
            Assert.Inconclusive("Test not implemented")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_image_maxWidth_maxHeight_NothingImage()
            'Arrange
            Dim success As Boolean = False
            Dim image As Image = Nothing

            Dim maxWidth As Integer = 100
            Dim maxHeight As Integer = 400

            'Act
            'ResizeImage arbeitet direkt auf dem uebergebenen Bild (ByRef) und gibt nichts zurueck.
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)
            success = image Is Nothing

            'Assert
            Assert.IsTrue(success, "Bei einem Nothing-Bild darf nichts veraendert werden")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_image_maxWidth_maxHeight_NothingSize()
            'Arrange
            'Die Zielgroesse kann heute kein Nothing sein (Integer-Parameter).
            '0 x 0 ist die kleinste ungueltige Angabe - das Bild muss unveraendert bleiben.
            Dim success As Boolean = False
            Dim image As Image = New Bitmap(My.Resources.TestPattern)
            Dim originalWidth As Integer = image.Width
            Dim originalHeight As Integer = image.Height

            Dim maxWidth As Integer = 0
            Dim maxHeight As Integer = 0

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)

            'Assert
            Dim widthOK = image.Width = originalWidth
            Dim heightOK = image.Height = originalHeight
            success = widthOK And heightOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success, "Bei einer ungueltigen Zielgroesse darf das Bild nicht veraendert werden")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_image_maxWidth_maxHeight_ZeroSize()
            'Arrange
            'Eine Breite von 0 ist ungueltig - das Bild muss unveraendert bleiben.
            Dim success As Boolean = False
            Dim image As Image = New Bitmap(My.Resources.TestPattern)
            Dim originalWidth As Integer = image.Width
            Dim originalHeight As Integer = image.Height

            Dim maxWidth As Integer = 0
            Dim maxHeight As Integer = 200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)

            'Assert
            Dim widthOK = image.Width = originalWidth
            Dim heightOK = image.Height = originalHeight
            success = widthOK And heightOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success, "Bei einer Breite von 0 darf das Bild nicht veraendert werden")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_image_maxWidth_maxHeight_NegativeSize()
            'Arrange
            'Negative Zielgroessen sind ungueltig - das Bild muss unveraendert bleiben.
            Dim success As Boolean = False
            Dim image As Image = New Bitmap(My.Resources.TestPattern)
            Dim originalWidth As Integer = image.Width
            Dim originalHeight As Integer = image.Height

            Dim maxWidth As Integer = -200
            Dim maxHeight As Integer = -200

            'Act
            ImageUtils.ResizeImage(image, maxWidth, maxHeight)

            'Assert
            Dim widthOK = image.Width = originalWidth
            Dim heightOK = image.Height = originalHeight
            success = widthOK And heightOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success, "Bei negativer Zielgroesse darf das Bild nicht veraendert werden")
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_image_maxWidth_maxHeight_Tall()
            'Arrange
            'Schmales Zielformat (100 x 400). Das Querformat-Bild wird nach Breite skaliert.
            Dim success As Boolean = False
            Dim image As Image = New Bitmap(My.Resources.TestPattern)
            Dim size As Size = New Size(100, 400)

            'Act
            ImageUtils.ResizeImage(image, size.Width, size.Height)

            'Assert
            If image Is Nothing Then Assert.Fail()
            'ResizeImage skaaliert proportional, damit das Bild in die Zielgroesse passt -
            'es darf dabei nicht gestreckt werden.
            Dim widthOK = image.Width <= size.Width
            Dim heightOK = image.Height <= size.Height
            Dim scaleOK = (image.Width = size.Width) OrElse (image.Height = size.Height)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is this image squished or stretched?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.No    'No means it is not stretched
            End Using
            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub
        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizeImage_image_maxWidth_maxHeight_Wide()
            'Arrange
            'Breites Zielformat (400 x 100). Das Hochformat-Bild wird nach Hoehe skaliert.
            Dim success As Boolean = False
            Dim image As Image = New Bitmap(My.Resources.TestPattern_Vert)
            Dim size As Size = New Size(400, 100)

            'Act
            ImageUtils.ResizeImage(image, size.Width, size.Height)

            'Assert
            If image Is Nothing Then Assert.Fail()
            Dim widthOK = image.Width <= size.Width
            Dim heightOK = image.Height <= size.Height
            Dim scaleOK = (image.Width = size.Width) OrElse (image.Height = size.Height)

            Dim visualOK = False
            Using dialog = New ImageFeedback()
                dialog.LoadInfo(image, "Is this image squished or stretched?")

                Dim userResponse = dialog.ShowDialog()
                visualOK = userResponse = Windows.Forms.DialogResult.No    'No means it is not stretched
            End Using
            success = widthOK And heightOK And scaleOK And visualOK
            image.Dispose()
            image = Nothing

            Assert.IsTrue(success)
        End Sub

        <InteractiveTest>
        <TestMethod()>
        Public Sub ImageUtils_AddGenreString()
            'Arrange
            Dim success As Boolean = False
            Using img As Image = My.Resources.Genre
                Dim genreString As String = "Fantasy"
                'Act
                Using result As Image = ImageUtils.AddGenreString(img, genreString)

                    'Assert
                    If result Is Nothing Then Assert.Fail()
                    Dim sizeOK = result.Size = img.Size

                    Dim visualOK = False
                    Using dialog = New ImageFeedback()
                        dialog.LoadInfo(result, "Does this image have " & genreString & " written on it?")

                        Dim userResponse = dialog.ShowDialog()
                        visualOK = userResponse = Windows.Forms.DialogResult.No    'No means it is not stretched
                    End Using
                    success = sizeOK And visualOK
                End Using
            End Using
        End Sub

    End Class
End Namespace
