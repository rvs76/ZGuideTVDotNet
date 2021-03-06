﻿' •————————————————————————————————————————————————————————————————————————————————————————————————————————————•
' |                                                                                                            |
' |    ZGuideTV.NET: An Electronic Program Guide (EPG) - i.e. an "electronic TV magazine"                      |
' |    - which makes the viewing of today's and next week's TV listings possible. It can be customized to      |
' |    pick up the TV listings you only want to have a look at. The application also enables you to carry out  |
' |    a search or even plan to record something later through the K!TV software.                              |
' |                                                                                                            |
' |    Copyright (C) 2004-2012 ZGuideTV.NET Team <http://zguidetv.codeplex.com/>                               |
' |                                                                                                            |
' |    Project administrator : Pascal Hubert (neojudgment@hotmail.com)                                         |
' |                                                                                                            |
' |    This program is free software: you can redistribute it and/or modify                                    |
' |    it under the terms of the GNU General Public License as published by                                    |
' |    the Free Software Foundation, either version 2 of the License, or                                       |
' |    (at your option) any later version.                                                                     |
' |                                                                                                            |
' |    This program is distributed in the hope that it will be useful,                                         |
' |    but WITHOUT ANY WARRANTY; without even the implied warranty of                                          |
' |    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                                           |
' |    GNU General Public License for more details.                                                            |
' |                                                                                                            |
' |    You should have received a copy of the GNU General Public License                                       |
' |    along with this program.  If not, see <http://www.gnu.org/licenses/>.                                   |
' |                                                                                                            |
' •————————————————————————————————————————————————————————————————————————————————————————————————————————————•
Imports System.Globalization
Imports ZGuideTV.SQLiteWrapper
Imports System.Net

Public Class ChannelView
    Dim _idChaine() As String
    Dim _icone() As String

    Private Sub programmechaine_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) _
        Handles Me.FormClosing

        ' raz de la variable
        IdentifiantLogo = ""
    End Sub

    Private Sub ProgrammechaineLoad(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load

        LanguageCheck()

        ' Par défaut c'est le boutton "Fermer" qui est appuyer quand on fait Enter
        AcceptButton = ChannelViewButtonClose

        ' remplisssage de combobox avec les chaine ainsi que les tableaux
        Dim strSql As String
        strSql = _
            "select channeltbl.name, channeltbl.id, channeltbl.logo, count(*) as cptemission, channeltbl.ordering from programtbl, channeltbl where (ProgramTbl.ChannelID= ChannelTbl.id) group by channeltbl.id order by ordering "
        Dim db As SQLiteBase = New SQLiteBase
        Dim dtChaines As DataTable
        db.OpenDatabase(BDDPath & "db_progs.db3")
        dtChaines = db.ExecuteQuery(strSql)
        db.CloseDatabase()
        db = Nothing

        ReDim _idChaine(dtChaines.Rows.Count)
        ReDim _icone(dtChaines.Rows.Count)

        For r As Integer = 0 To dtChaines.Rows.Count - 1
            cbo_Chaine.Items.Add(dtChaines.Rows(r).Item(0).ToString)
            _idChaine(r) = dtChaines.Rows(r).Item(1).ToString
            _icone(r) = dtChaines.Rows(r).Item(2).ToString
        Next

        ' si 1'identifiant de chaine est trouvé, on force la selection dans la
        ' combobox et on force le chargement de la journée
        If Not (Not IdentifiantLogo Is Nothing AndAlso String.IsNullOrEmpty(IdentifiantLogo)) Then
            For i As Integer = 0 To cbo_Chaine.Items.Count
                If _idChaine(i) = IdentifiantLogo Then
                    If _
                        moment_souhaite.ToString("yyyyMMdd", CultureInfo.CurrentCulture) = _
                        DateTime.Now.AddHours(-decal_horaire).ToString("yyyyMMdd", CultureInfo.CurrentCulture) Then
                        ChannelViewCheckBox24hHours.Checked = True
                        cbo_Chaine.SelectedIndex = i

                    Else
                        cbo_Chaine.SelectedIndex = i
                        dtp_Jour.Value = moment_souhaite

                    End If
                    ' cbo_Chaine.SelectedIndex = i
                    Exit For
                End If
            Next

            ' sinon on selectionne la premiere chaine de la liste
        Else
            cbo_Chaine.SelectedIndex = 0
        End If
        If ChannelListView.Items.Count > 0 Then
            ChannelListView.Focus()
            ChannelListView.Items(0).Selected = True
        End If
    End Sub

    Private Sub BtFermerClick(ByVal sender As Object, ByVal e As EventArgs) Handles ChannelViewButtonClose.Click
        Me.Close()
    End Sub

    Private Sub ChannelListViewMouseDoubleClick(ByVal sender As Object, ByVal e As MouseEventArgs) _
        Handles ChannelListView.MouseDoubleClick

        If ChannelListView.SelectedItems.Count = 1 Then

            Try
                If IsNetConnectOnline() Then
                    ThetvdbName = ChannelListView.Items(ChannelListView.SelectedIndices.Item(0)).SubItems(2).Text
                    TopMost = False
                    SuspendLayout()
                    My.Forms.SeriesBrowser.ShowDialog()
                    ResumeLayout()
                    TopMost = True
                Else

                    ' Message indiquant qu'il n'y a pas de connexion internet dispo pour afficher
                    ' la fiche IMDB
                    Dim BoxNoConnection As DialogResult
                    BoxNoConnection = _
                        MessageBox.Show( _
                                         Central_Screen.MessageBoxNoConnection & Chr(13) & _
                                         Central_Screen.MessageBoxNoConnection1, _
                                         Central_Screen.MessageBoxNoConnectionTitre, _
                                         MessageBoxButtons.OK, _
                                         MessageBoxIcon.Exclamation)
                End If

            Catch ex As WebException

                Dim BoxNoConnection As DialogResult
                BoxNoConnection = _
                    MessageBox.Show( _
                                     Central_Screen.MessageBoxNoConnection & Chr(13) & _
                                     Central_Screen.MessageBoxNoConnection1, Central_Screen.MessageBoxNoConnectionTitre, _
                                     MessageBoxButtons.OK, _
                                     MessageBoxIcon.Exclamation)
            End Try

        End If

    End Sub

    Private Sub LwEmissionSelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) _
        Handles ChannelListView.SelectedIndexChanged

        ' sur le click d'un item de listview, on affiche la description
        If ChannelListView.SelectedItems.Count = 1 Then
            rtb_desc.Text = ChannelListView.SelectedItems(0).Tag.ToString
        End If
    End Sub

    Private Sub AfficheJournee()

        Dim reste As Integer
        Dim strSql As String
        Dim debut As String
        Dim fin As String
        Dim test As Integer = My.Settings.decalage_horaire

        If ChannelViewCheckBox24hHours.Checked Then
            debut = DateTime.Now.AddHours(-decal_horaire).ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)
            fin = DateAdd("d", 1, DateTime.Now.AddHours(-decal_horaire)).ToString("yyyy-MM-dd HH:mm")
        Else
            debut = dtp_Jour.Value.ToString("yyyy-MM-dd 00:00", CultureInfo.CurrentCulture)
            fin = dtp_Jour.Value.ToString("yyyy-MM-dd 23:59", CultureInfo.CurrentCulture)
        End If

        ' recuperation des emissions de la journee pour une chaine dans la base
        strSql = "select pstart, pstop, ptitle, pcategorytv, pdescription from programTbl where channelid='" & _
                 _idChaine(cbo_Chaine.SelectedIndex) & "' AND ((PStart Between '" & debut & "' and '" & fin & _
                 "') or (pstop between '" & debut & "' and '" & fin & "'))"
        Dim db As SQLiteBase = New SQLiteBase
        Dim dtEmissions As DataTable
        db.OpenDatabase(BDDPath & "db_progs.db3")
        dtEmissions = db.ExecuteQuery(strSql)
        db.CloseDatabase()
        db = Nothing

        ' nettoyage de la listview et de la richtextbox
        ChannelListView.Items.Clear()
        rtb_desc.Text = ""

        ' affichage du logo de la chaine
        pb_chaine.Image = Image.FromFile(LogosPath & _icone(cbo_Chaine.SelectedIndex))

        ' remplissage de la listview
        For r As Integer = 0 To dtEmissions.Rows.Count - 1

            'Dim EmissionItem As New ListViewItem(CDate(dt_Emissions.Rows(r).Item(0)).ToString("HH:mm"))
            Dim a As Object
            a = dtEmissions.Rows(r).Item(0)
            Dim b As New DateTime
            b = CDate(a)
            b = b.AddHours(decal_horaire)
            Dim emissionItem As New ListViewItem(CDate(b).ToString("HH:mm"))

            'EmissionItem.SubItems.Add(CDate(dt_Emissions.Rows(r).Item(1)).ToString("HH:mm"))
            a = dtEmissions.Rows(r).Item(1)
            b = CDate(a)
            b = b.AddHours(decal_horaire)
            emissionItem.SubItems.Add((CDate(b).ToString("HH:mm", CultureInfo.CurrentCulture)))
            'BB 170310EmissionItem.SubItems.Add(CDate(dt_Emissions.Rows(r).Item(1)).ToString("HH:mm"))

            With emissionItem
                .SubItems.Add(dtEmissions.Rows(r).Item(2).ToString)
                .SubItems.Add(dtEmissions.Rows(r).Item(3).ToString)
                .Tag = dtEmissions.Rows(r).Item(4)
            End With

            ' change la couleur de fond 1 item sur 2
            Math.DivRem(r, 2, reste)
            If (reste = 0) Then
                emissionItem.BackColor = Color.WhiteSmoke

            End If
            ChannelListView.Items.Add(emissionItem)
        Next
        If ChannelListView.Items.Count > 0 Then
            ChannelListView.Items(0).Selected = True
        End If
    End Sub

    Private Sub Ckb24HeuresCheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
        Handles ChannelViewCheckBox24hHours.CheckedChanged
        dtp_Jour.Enabled = Not (ChannelViewCheckBox24hHours.Checked)
    End Sub

    Private Sub CboChaineSelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) _
        Handles cbo_Chaine.SelectedIndexChanged
        AfficheJournee()
    End Sub

    Private Sub DtpJourValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles dtp_Jour.ValueChanged
        AfficheJournee()
    End Sub

    Private Sub ChannelViewCheckBox24HHoursClick(ByVal sender As Object, ByVal e As EventArgs) _
        Handles ChannelViewCheckBox24hHours.Click
        AfficheJournee()
    End Sub
End Class