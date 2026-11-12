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
' # but WITHOUT ANY WARRANTY; without even the implied warranty of                #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ################################################################################
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports EmberAPI
Imports UnitTests

Namespace EmberTests

    ''' <summary>
    ''' Regressionstests fuer den Use-after-dispose-Absturz im Bild-Paint-Pfad.
    '''
    ''' Hintergrund: Beim Scrapen von "Doctor Who" stuerzte das Programm am
    ''' 10.11.2026 um 00:27:07 mit folgendem Stack ab:
    '''
    '''   System.ArgumentException: Ungueltiger Parameter.
    '''    bei System.Drawing.Image.get_Width()
    '''    bei System.Drawing.Image.get_Size()
    '''    bei System.Windows.Forms.PictureBox.ImageRectangleFromSizeMode(...)
    '''    bei System.Windows.Forms.PictureBox.OnPaint(PaintEventArgs pe)
    '''
    ''' Ursache: Die PictureBox hielt eine Image-Instanz, deren GDI+-Handle bereits
    ''' freigegeben war. Ein Paint auf ein disposed Image laesst Image.get_Width
    ''' mit "Ungueltiger Parameter" fehlschlagen.
    '''
    ''' Zwei Mechanismen haben das ausgeloest:
    ''' 1. ImageUtils.ResizePB gab das alte Image der Ziel-PictureBox frei, BEVOR
    '''    das neue zugewiesen wurde. In diesem Fenster hielt die PictureBox eine
    '''    disposed Image-Referenz. Ausgeloest wurde das dadurch, dass Quelle und
    '''    Ziel dasselbe Image teilten.
    ''' 2. Der Images-Container und die PictureBox teilten sich dieselbe
    '''    Image-Instanz (frmMain: pbActors.Image = MainActors.Image). Ein
    '''    Clear() des Containers - im Worker-Thread - gab das Image frei, waehrend
    '''    die PictureBox es noch anzeigte.
    '''
    ''' Diese Tests sichern beide Mechanismen ab.
    ''' </summary>
    <TestClass()>
    Public Class Test_clsAPIImageUtils_UseAfterDispose

        ''' <summary>
        ''' Paint auf eine PictureBox erzwingen. PictureBox.OnPaint ist protected,
        ''' deshalb wird ueber eine kleine Ableitung gearbeitet.
        ''' </summary>
        Private Class PaintProbe
            Inherits PictureBox

            Public Sub ForcePaint()
                Using surf As New Bitmap(50, 50)
                    Using g As Graphics = Graphics.FromImage(surf)
                        OnPaint(New PaintEventArgs(g, New Rectangle(0, 0, 50, 50)))
                    End Using
                End Using
            End Sub
        End Class

        ''' <summary>
        ''' Paint auf eine PictureBox erzwingen und einen Fehler melden, statt ihn
        ''' zu verschlucken. So laesst sich der GDI+-Fehler direkt ausloesen.
        ''' </summary>
        Private Shared Function PaintForError(pb As PictureBox) As Exception
            Dim probe As New PaintProbe()
            probe.SizeMode = pb.SizeMode
            probe.Image = pb.Image
            probe.Width = pb.Width
            probe.Height = pb.Height

            Try
                probe.ForcePaint()
                Return Nothing
            Catch ex As Exception
                Return ex
            End Try
        End Function

        ''' <summary>
        ''' Ein Paint auf eine PictureBox, deren Image freigegeben wurde, muss mit
        ''' dem GDI+-Fehler "Ungueltiger Parameter" fehlschlagen. Dieser Test
        ''' haelt fest, dass genau dieses Fehlerbild der Grund des Absturzes war.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub PictureBox_Paint_MitDisposedImage_ErzeugtGDIPlusFehler()
            'Arrange
            Dim probe As New PaintProbe()
            probe.SizeMode = PictureBoxSizeMode.AutoSize
            Dim bitmap As New Bitmap(10, 10)
            probe.Image = bitmap
            bitmap.Dispose()

            'Act
            Dim fehler As Exception = Nothing
            Try
                probe.ForcePaint()
            Catch ex As Exception
                fehler = ex
            End Try

            'Assert
            Assert.IsNotNull(fehler, "Erwartet wurde der GDI+-Fehler, Paint lief aber fehlerfrei durch")
            Assert.IsInstanceOfType(fehler, GetType(ArgumentException), "Erwartet wurde ein ArgumentException")
            Assert.AreEqual("Ungültiger Parameter.", fehler.Message, "Der GDI+-Fehler hat einen anderen Text")
        End Sub

        ''' <summary>
        ''' ResizePB: wenn Quelle und Ziel dasselbe Image teilen, darf das
        ''' freigegebene Quell-Image nicht den ganzen Aufruf abbrechen.
        '''
        ''' Vor dem Fix wurde das Image der Ziel-PictureBox am Anfang disposed -
        ''' das war in diesem Szenario das Quell-Image. Der anschliessende Zugriff
        ''' auf pbSource.Image.Height schlug mit "Ungueltiger Parameter" fehl und
        ''' die PictureBox behielt ein totes Image.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizePB_GeteiltesImage_DarfNichtAbbrechen()
            'Arrange
            Dim source As New PictureBox()
            Dim destination As New PictureBox()

            Using bitmap As New Bitmap(40, 60)
                'Quelle und Ziel teilen sich dieselbe Instanz - so teilen sich
                'in frmMain der Images-Container und die Cache-PictureBox das
                'Image.
                source.Image = New Bitmap(bitmap)
                destination.Image = source.Image

                'Act
                ImageUtils.ResizePB(destination, source, 100, 100)

                'Assert: Die Ziel-PictureBox muss ein lebendes Image haben.
                Assert.IsNotNull(destination.Image, "Die PictureBox hat kein Image mehr")
                Assert.IsFalse(destination.Image.Width <= 0, "Das Image der PictureBox ist bereits freigegeben")
            End Using
        End Sub

        ''' <summary>
        ''' Und derselbe Fall als Paint - das ist der Absturzpfad.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizePB_GeteiltesImage_DarfNichtGepaintetWerden()
            'Arrange
            Dim source As New PictureBox()
            Dim destination As New PictureBox()

            Using bitmap As New Bitmap(40, 60)
                source.Image = New Bitmap(bitmap)
                destination.Image = source.Image

                'Act
                ImageUtils.ResizePB(destination, source, 100, 100)

                'Assert
                Dim fehler As Exception = PaintForError(destination)
                Assert.IsNull(fehler, "Paint auf ein freigegebenes Image schlug fehl: " & If(fehler Is Nothing, "", fehler.Message))
            End Using
        End Sub

        ''' <summary>
        ''' ResizePB darf die Ziel-PictureBox nicht mit einem bereits
        ''' freigegebenen Image zuruecklassen.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_ResizePB_ZielImageIstNachAufrufNochLebendig()
            'Arrange
            Dim source As New PictureBox()
            Dim destination As New PictureBox()

            Using sourceBitmap As New Bitmap(40, 60)
                source.Image = New Bitmap(sourceBitmap)

                'Die Ziel-PictureBox bekommt vorher schon ein Image, damit
                'ResizePB den "alten" Image-Zweig ueberhaupt betritt.
                destination.Image = New Bitmap(80, 120)

                'Act
                ImageUtils.ResizePB(destination, source, 100, 100)

                'Assert: Das Image der PictureBox darf nicht disposed sein.
                Assert.IsNotNull(destination.Image, "Die PictureBox hat kein Image mehr")
                Assert.IsFalse(destination.Image.Width <= 0, "Das Image der PictureBox ist bereits freigegeben")
            End Using
        End Sub

        ''' <summary>
        ''' SetGlassOverlay hatte denselben Defekt: das alte Image wurde verworfen,
        ''' ohne dass die PictureBox ein lebendes Image behielt.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub ImageUtils_SetGlassOverlay_ZielImageIstNachAufrufNochLebendig()
            'Arrange
            Dim underlay As New PictureBox()
            Using bitmap As New Bitmap(40, 60)
                underlay.Image = New Bitmap(bitmap)

                'Act
                ImageUtils.SetGlassOverlay(underlay)

                'Assert
                Assert.IsNotNull(underlay.Image, "Die PictureBox hat kein Image mehr")
                Assert.IsFalse(underlay.Image.Width <= 0, "Das Image der PictureBox ist bereits freigegeben")
            End Using
        End Sub

        ''' <summary>
        ''' Ownership-Regel: mit ReleaseImage gibt der Container die Verfuegung
        ''' ueber das Image ab. Ein spaeteres Clear() des Containers darf dieses
        ''' Image dann nicht mehr freigeben - die PictureBox zeigt es ja noch.
        '''
        ''' So wird der Absturz vermieden, ohne dass das Image des Containers
        ''' verloren geht.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub Images_ReleaseImage_ClearDarfDasFreigegebeneImageNichtFreigeben()
            'Arrange
            Dim container As New Images()
            Dim pictureBox As New PictureBox()

            Using bitmap As New Bitmap(40, 60)
                container.UpdateMSfromImg(New Bitmap(bitmap))

                'Act: Die PictureBox uebernimmt die Verfuegung ueber das Image.
                pictureBox.Image = container.ReleaseImage()
                container.Clear()

                'Assert: Das Image der PictureBox muss noch leben.
                Assert.IsNotNull(pictureBox.Image, "Die PictureBox hat kein Image mehr")
                Assert.IsFalse(pictureBox.Image.Width <= 0, "Das Image der PictureBox ist bereits freigegeben")

                Dim fehler As Exception = PaintForError(pictureBox)
                Assert.IsNull(fehler, "Paint auf ein freigegebenes Image schlug fehl: " & If(fehler Is Nothing, "", fehler.Message))
            End Using
        End Sub

        ''' <summary>
        ''' ReleaseImage gibt das Image genau einmal heraus. Ein zweiter Aufruf
        ''' liefert Nothing - sonst wuerde dieselbe Instanz an zwei PictureBoxen
        ''' verteilt.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub Images_ReleaseImage_GibtDasImageNurEinmalHeraus()
            'Arrange
            Dim container As New Images()

            Using bitmap As New Bitmap(40, 60)
                container.UpdateMSfromImg(New Bitmap(bitmap))

                'Act
                Dim erstes As Image = container.ReleaseImage()
                Dim zweites As Image = container.ReleaseImage()

                'Assert
                Assert.IsNotNull(erstes, "Das erste ReleaseImage muss das Image liefern")
                Assert.IsNull(zweites, "Das zweite ReleaseImage darf das Image nicht erneut liefern")
            End Using
        End Sub

        ''' <summary>
        ''' Gegenprobe: gibt der Container das Image nicht heraus, bleibt er
        ''' fuer dessen Freigabe zustaendig. Das verhindert, dass durch die
        ''' Ownership-Regel GDI+-Handles leaken.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub Images_Clear_GibtDasEigeneImageFrei()
            'Arrange
            Dim container As New Images()
            Dim image As Image = Nothing
            Dim wurdeFreigegeben As Boolean = False

            Using bitmap As New Bitmap(40, 60)
                container.UpdateMSfromImg(New Bitmap(bitmap))
                image = container.Image

                'Act
                container.Clear()

                'Assert: Das Image des Containers wurde freigegeben. Ein
                'disposetes Image laesst das Auslesen der Breite mit
                '"Ungueltiger Parameter" fehlschlagen.
                Try
                    Dim w As Integer = image.Width
                Catch ex As ArgumentException
                    wurdeFreigegeben = True
                End Try

                Assert.IsTrue(wurdeFreigegeben, "Das Image des Containers wurde nicht freigegeben")
            End Using
        End Sub

    End Class

End Namespace
