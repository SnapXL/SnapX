using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SnapX.Core;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.Views.Controls;


public partial class Donation : UserControl
{
    [RelayCommand]
    private void PrimaryClick()
    {
        DebugHelper.WriteLine("PrimaryClick");
        URLHelpers.OpenURL(donationURL);
    }
    public Donation()
    {
        InitializeComponent();
    }
    private static bool _isInitialized;

    private static bool shownEffect;
    // ALWAYS IN USD
    // Decimal because Jeff Bezos definitely wants to donate more, but let's start with a humble $15
    private decimal donationAmount = 15;
    private string selectedService = "";
    private string donationURL { get; set; } = "";
    private void DonationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            DebugHelper.WriteLine("DonationComboBox_SelectionChanged first run!");
            _isInitialized = true;
            return;
        }

        var comboBox = sender as FAComboBox;
        ProcessDonation(comboBox);
    }

    private void ProcessDonation(FAComboBox comboBox, bool isCustomInput = false)
    {
        var text = comboBox.Text;
        DebugHelper.WriteLine($"ProcessDonation, {text}");
        var selectedAmount = donationAmount;
        if (comboBox.SelectedItem is FAComboBoxItem { Tag: not null } selectedItem)
        {
            // Check if user is still using selected item or typed something else
            if (string.Equals(text, selectedItem.Content as string, StringComparison.InvariantCultureIgnoreCase))
            {
                if (decimal.TryParse(selectedItem.Tag.ToString(), out var amountFromTag))
                {
                    selectedAmount = amountFromTag;
                    isCustomInput = false;
                }
            }
        }

        if (isCustomInput)
        {

            text = text.Replace("USD", "", StringComparison.InvariantCultureIgnoreCase)
                .Replace("$", "")
                .Trim();

            if (decimal.TryParse(text, NumberStyles.Currency, CultureInfo.InvariantCulture, out var customAmount))
            {
                selectedAmount = customAmount;
            }
        }


        if (selectedAmount >= 15)
        {
            ShowSparkleEffect();
        }
        else
        {
            ResetSparkleEffect();
        }
        donationAmount = selectedAmount;
        UpdateDonationURL();
    }

    // Placeholder method for sparkle effect (could be animation)
    private void ShowSparkleEffect()
    {
        if (shownEffect) return;
        shownEffect = true;
        DebugHelper.WriteLine("Imagine sparkles effect");
    }

    private void ResetSparkleEffect()
    {
        shownEffect = false;
    }


    private void DonationServiceComboBox(object? Sender, SelectionChangedEventArgs E)
    {
        if (Sender is not FAComboBox comboBox) return;
        var comboBoxItem = comboBox.SelectedItem as FAComboBoxItem;
        var serviceName = comboBoxItem!.Content as string;
        selectedService = serviceName!;
        UpdateDonationURL();
    }
    private void UpdateDonationURL()
    {
        if (string.IsNullOrEmpty(selectedService)) return;

        donationURL = selectedService switch
        {
            "GitHub Sponsors" =>
                $"https://github.com/sponsors/BrycensRanch/sponsorships?preview=false&frequency=one-time&amount={donationAmount}",
            "Ko-fi" => $"https://ko-fi.com/BrycensRanch/{donationAmount}",
            "Liberapay" =>
                $"https://liberapay.com/BrycensRanch/donate?amount={donationAmount}&currency=USD&period=yearly",
            _ => donationURL
        };
    }


    private void DonationComboBox_OnPointerExited(object? Sender, PointerEventArgs E)
    {
        ProcessDonation(Sender as FAComboBox);
    }

    private void DonationComboBox_OnKeyUp(object? Sender, KeyEventArgs E)
    {
        ProcessDonation(Sender as FAComboBox, true);
    }
}

