using System.Collections.ObjectModel;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FootballManager.FootballDataSetTableAdapters;
using System.Data;
using System.Collections;
using System.ComponentModel;
using System;
using System.Net.Mail;

namespace FootballManager
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<PlayerMatch> PlayerMatches { get; set; }
        public ObservableCollection<string> Teams { get; set; }
        public ObservableCollection<string> Players { get; set; }
        public List<Match> Matches { get; set; }
        public FootballDataSet footballDataSet = new FootballDataSet();
        public PlayerMatchTableAdapter taPlayerMatches = new PlayerMatchTableAdapter();
        public PlayerTableAdapter taPlayers = new PlayerTableAdapter();
        public TeamTableAdapter taTeams = new TeamTableAdapter();
        public MatchTableAdapter taMatches = new MatchTableAdapter();
        public ConfigTableAdapter taConfig = new ConfigTableAdapter();
        public PlayerStatsTableAdapter taPlayerStats = new PlayerStatsTableAdapter();

        private int maxNoOfPlayers;
        private int maxPlayersInATeam;
        private int defaultMatchId;
        private string cryptoPhrase;
        private int selectedMatchId;
        private int defaultSelectedIndex;
        private FootballDataSet.ConfigRow configRow;

        public MainWindow()
        {
            InitializeComponent();
            InitializeFootballDataSet();
            InitializeVariables();
            InitializeInterfaceCollections();
            InitializeBindings();
        }

        private void InitializeVariables()
        {
            maxNoOfPlayers = 10;
            maxPlayersInATeam = maxNoOfPlayers / 2;
            defaultMatchId = FootballHelper.GetDefaultMatchId(footballDataSet.Match);
            defaultSelectedIndex = 0;
            selectedMatchId = defaultMatchId;
            cryptoPhrase = "something";
            configRow = (FootballDataSet.ConfigRow)footballDataSet.Config.Select().Single();
        }

        private void InitializeFootballDataSet()
        {
            UpdateAndSortPlayerMatchDataTable();
            UpdateAndSortPlayerDataTable();
            UpdateAndSortTeamDataTable();
            UpdateMatchDataTable();
            UpdateConfigDataTable();
            UpdateAndSortPlayerStatsDataTable();
        }

        private void SortPlayerStatsDataTable()
        {
            footballDataSet.PlayerStats.DefaultView.Sort = "MatchWins DESC";
        }

        private void SortPlayerDataTable()
        {
            footballDataSet.Player.DefaultView.Sort = "PlayerName ASC";
        }

        private void SortPlayerMatchDataTable()
        {
            footballDataSet.PlayerMatch.DefaultView.Sort = "TeamID ASC";
        }

        private void SortTeamDataTable()
        {
            footballDataSet.Team.DefaultView.Sort = "TeamName ASC";
        }

        private void UpdateAndSortPlayerMatchDataTable()
        {
            taPlayerMatches.Fill(footballDataSet.PlayerMatch);
            SortPlayerMatchDataTable();
        }

        private void UpdateAndSortPlayerDataTable()
        {
            taPlayers.Fill(footballDataSet.Player);
            SortPlayerDataTable();
        }

        private void UpdateAndSortTeamDataTable()
        {
            taTeams.Fill(footballDataSet.Team);
            SortTeamDataTable();
        }

        private void UpdateMatchDataTable()
        {
            taMatches.Fill(footballDataSet.Match);
        }

        private void UpdateConfigDataTable()
        {
            taConfig.Fill(footballDataSet.Config);
        }

        private void UpdateAndSortPlayerStatsDataTable()
        {
            taPlayerStats.Fill(footballDataSet.PlayerStats);
            SortPlayerStatsDataTable();
        }

        private void InitializeInterfaceCollections()
        {
            Teams = FootballHelper.GetTeams(footballDataSet.Team);
            Players = FootballHelper.GetPlayers(footballDataSet.Player);
            PlayerMatches = FootballHelper.GetPlayerMatches(footballDataSet, -1);
            Matches = FootballHelper.GetRelevantMatches(footballDataSet.Match);
        }

        private void InitializeBindings()
        {
            cb_Team.ItemsSource = Teams;
            cb_Player.ItemsSource = Players;
            dg_PlayerMatch.ItemsSource = FootballHelper.GetPlayerMatches(footballDataSet, defaultMatchId);
            dg_PlayerMatch.Items.SortDescriptions.Add(new SortDescription("TeamName", ListSortDirection.Ascending));
            lb_Matches.ItemsSource = Matches;
            lb_Matches.Items.SortDescriptions.Add(new SortDescription("MatchDate", ListSortDirection.Ascending));
            cb_MatchWinner.ItemsSource = Teams;
            cb_MatchWinner.SelectedIndex = FootballHelper.GetMatchWinnerIndexByMatchID(footballDataSet.Match, selectedMatchId);
        }

        private void lb_Matches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Match selection = (Match)lb_Matches.SelectedItem;

            ObservableCollection<PlayerMatch> currentPlayerMatchs = FootballHelper.GetPlayerMatches(footballDataSet, selection.MatchID);

            int noOfBlankPlayersToAdd = maxNoOfPlayers - currentPlayerMatchs.Count();

            while (noOfBlankPlayersToAdd != 0)
            {
                currentPlayerMatchs.Add(new PlayerMatch());
                noOfBlankPlayersToAdd--;
            }

            dg_PlayerMatch.ItemsSource = currentPlayerMatchs;
            cb_MatchWinner.SelectedIndex = FootballHelper.GetMatchWinnerIndexByMatchID(footballDataSet.Match, selection.MatchID);

            selectedMatchId = selection.MatchID;
        }

        private void btn_Update_Click(object sender, RoutedEventArgs e)
        {
            if (gridSelectionIsValid())
                UpdatePlayerMatchResourcesAndDataGrid();
            else
                SendErrorToUser();
        }

        private bool gridSelectionIsValid()
        {
            if (GridRowIncomplete())
                return false;

            if (PlayerAppearsMoreThanOnce())
                return false;

            if (MoreThanMaxPlayersInATeam())
                return false;

            return true;
        }

        private void SendErrorToUser()
        {
            string errorMessage = GetErrorMessageOnUpdate();
            MessageBox.Show(errorMessage);
        }

        private void UpdatePlayerMatchResourcesAndDataGrid()
        {
            UpdatePlayerMatchResources();
            UpdatePlayerMatchDataGrid();
        }

        private void UpdatePlayerMatchResources()
        {
            // Delete
            FootballHelper.DeletePlayerMatches(taPlayerMatches, selectedMatchId);

            // Fill from Selection
            List<PlayerMatch> playerMatches = GetPlayerMatchesToInsertFromGrid();
            playerMatches = PopulateMissingPlayerMatchData(playerMatches);

            // Insert
            foreach (PlayerMatch playerMatch in playerMatches)
                taPlayerMatches.Insert(playerMatch.PlayerID, playerMatch.MatchID, playerMatch.TeamID);

            UpdateMatchWinner();
            UpdateAndSortPlayerStatsDataTable();
            UpdateAndSortPlayerMatchDataTable();
        }

        private List<PlayerMatch> GetPlayerMatchesToInsertFromGrid()
        {
            List<PlayerMatch> playerMatchesToInsert = new List<PlayerMatch>();

            foreach (PlayerMatch playerMatch in GetFullDataGridSelection())
            {
                if (FootballHelper.PlayerMatchCompletedByUser(playerMatch))
                    playerMatchesToInsert.Add(playerMatch);
            }
            return playerMatchesToInsert;
        }

        private List<PlayerMatch> PopulateMissingPlayerMatchData(List<PlayerMatch> currentPlayerMatches)
        {
            List<PlayerMatch> completePlayerMatches = new List<PlayerMatch>();

            foreach (PlayerMatch playerMatch in currentPlayerMatches)
            {
                playerMatch.MatchID = selectedMatchId;
                playerMatch.PlayerID = FootballHelper.GetPlayerIDFromName(footballDataSet.Player, playerMatch.PlayerName);
                playerMatch.TeamID = FootballHelper.GetTeamIDFromName(footballDataSet.Team, playerMatch.TeamName);
                completePlayerMatches.Add(playerMatch);
            }
            return completePlayerMatches;
        }

        private List<string> GetPlayerNamesFromDataGrid()
        {
            List<string> playerNames = new List<string>();

            foreach (PlayerMatch gridRow in GetFullDataGridSelection())
                if (!string.IsNullOrEmpty(gridRow.PlayerName))
                    playerNames.Add(gridRow.PlayerName);

            return playerNames;
        }

        private List<string> GetTeamNamesFromDataGrid()
        {
            List<string> teamNames = new List<string>();

            foreach (PlayerMatch gridRow in GetFullDataGridSelection())
                if (!string.IsNullOrEmpty(gridRow.TeamName))
                    teamNames.Add(gridRow.TeamName);

            return teamNames;
        }

        private bool GridRowIncomplete()
        {
            foreach (PlayerMatch gridRow in GetFullDataGridSelection())
            {
                if (FootballHelper.RowHasTeamButNoPlayer(gridRow))
                    return true;

                if (FootballHelper.RowHasPlayerButNoTeam(gridRow))
                    return true;
            }
            return false;
        }

        public string GetErrorMessageOnUpdate()
        {
            if (GridRowIncomplete())
                return "Either the team or the player is missing for one of the entries.";

            if (PlayerAppearsMoreThanOnce())
                return "One of the selected players appears more than once for this match.";

            if (MoreThanMaxPlayersInATeam())
                return "One of the teams has 6 players.";

            return string.Empty;
        }

        private bool MoreThanMaxPlayersInATeam()
        {
            List<string> teamNames = GetTeamNamesFromDataGrid();
            string firstTeamName = FootballHelper.GetFirstTeamName(footballDataSet.Team);
            string lastTeamName = FootballHelper.GetLastTeamName(footballDataSet.Team);

            int totalOnFirstTeam = GetTotalOnTeam(teamNames, firstTeamName);
            int totalOnLastTeam = GetTotalOnTeam(teamNames, firstTeamName);

            if (totalOnFirstTeam > maxPlayersInATeam)
                return true;

            if (totalOnLastTeam > maxPlayersInATeam)
                return true;

            return false;
        }

        private int GetTotalOnTeam(List<string> allTeamNames, string teamName)
        {
            return allTeamNames.Count(n => n == teamName);
        }

        private bool PlayerAppearsMoreThanOnce()
        {
            List<string> playerNames = GetPlayerNamesFromDataGrid();

            var duplicateNames = from x in playerNames
                                 group x by x into grouped
                                 where grouped.Count() > 1
                                 select grouped.Key;

            if (duplicateNames.Count() > 0)
                return true;

            return false;
        }

        private IList GetFullDataGridSelection()
        {
            dg_PlayerMatch.SelectAll();
            return (IList)dg_PlayerMatch.SelectedItems;       
        }

        private void UpdateMatchWinner()
        {
            if (cb_MatchWinner.SelectedItem == null)
                return;

            int selectedMatchWinner = FootballHelper.GetTeamIDFromName(footballDataSet.Team, cb_MatchWinner.SelectedItem.ToString());
            FootballDataSet.MatchRow rowToUpdate = (FootballDataSet.MatchRow)footballDataSet.Match.Select("MatchID = " + selectedMatchId).Single();
            rowToUpdate.MatchWinner = selectedMatchWinner;
            taMatches.Update(rowToUpdate);
        }

        private void UpdatePlayerMatchDataGrid()
        {
            dg_PlayerMatch.Items.Refresh();
            dg_PlayerMatch.SelectedIndex = defaultSelectedIndex;
        }

        private void SendEmail(string body)
        {
            SmtpClient SmtpServer = new SmtpClient(configRow["SmtpServer"].ToString());
            var mail = new MailMessage();
            mail.From = new MailAddress(GetAgentSine());
            mail.To.Add(GetAgentSine());
            mail.Subject = DateTime.Now.ToString("yyyy-MM-dd") + " - Thursday Football Stats";
            mail.Body = body;
            SmtpServer.Port = int.Parse(configRow["SmtpPort"].ToString());
            SmtpServer.UseDefaultCredentials = false;
            SmtpServer.Credentials = new System.Net.NetworkCredential(GetAgentSine(), GetDecryptedAgentDutyCode());
            SmtpServer.EnableSsl = true;
            SmtpServer.Send(mail);
        }

        private void btn_SendEmail_Click(object sender, RoutedEventArgs e)
        {
            DataView rawData = footballDataSet.PlayerStats.DefaultView;
            string formattedData = FootballHelper.FormatStatsDataToText(rawData);
            SendEmail(formattedData);
            MessageBox.Show("Your e-mail has been sent!");
            // TODO - Tidy Up
            // TODO - Exception Handling

        }

        private string GetDecryptedAgentDutyCode()
        {
            return Decrypt(configRow["SmtpAgentDutyCode"].ToString());
        }

        private string GetAgentSine()
        {
            return configRow["SmtpAgentSine"].ToString();
        }

        private string Encrypt(string inputString)
        {
            return StringCipher.Encrypt(inputString, cryptoPhrase);
        }

        private string Decrypt(string inputString)
        {
            return StringCipher.Decrypt(inputString, cryptoPhrase);
        }

        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            {
                if (!cell.IsFocused)
                {
                    cell.Focus();
                }
                DataGrid dataGrid = FindVisualParent<DataGrid>(cell);
                if (dataGrid != null)
                {
                    if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected)
                            cell.IsSelected = true;
                    }
                    else
                    {
                        DataGridRow row = FindVisualParent<DataGridRow>(cell);
                        if (row != null && !row.IsSelected)
                        {
                            row.IsSelected = true;
                        }
                    }
                }
            }
        }

        static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private void cb_Player_Loaded(object sender, RoutedEventArgs e)
        {
            ((ComboBox)sender).IsDropDownOpen = true;
        }
    }
}