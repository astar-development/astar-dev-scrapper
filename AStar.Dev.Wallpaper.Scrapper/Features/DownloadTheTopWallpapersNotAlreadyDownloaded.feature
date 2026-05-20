Feature: DownloadTheTopWallpapersNotAlreadyDownloaded

A short summary of the feature

    @tag1
    Scenario: I can download all of the Top Wallpapers I've not already got
        Given I access the website
        When I enter valid credentials
        Then I can see that I have logged in successfully
        And I can download the top wallpapers I do not already have