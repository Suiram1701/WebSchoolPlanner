"use strict";

const settingsUrl = "/api/v1/account/settings"

$().ready(function () {

    // Language switch
    $("a[id='userLanguageSwitch']").each(function () {
        let element = $(this);
        const elementCulture = element.attr("data-culture");

        if (element.hasClass("active"))     // Abort when current language
            return;

        element.click(function () {
            let content = {
                culture: elementCulture
            }

            $.ajax({
                type: "PUT",
                url: settingsUrl,
                dataType: "json",
                contentType: "application/json",
                data: JSON.stringify(content),
                error: function (data) {     // Always executed

                    // Error
                    if (data.status !== 200) {
                        LogAjaxError(data);
                        ShowAjaxErrorBox(data);
                        return;
                    }

                    document.location.reload();
                }
            });
        });
    });

    // color theme switch
    $("button[id='userColorThemeSwitch']").each(function () {
        let element = $(this);
        let elementTheme = element.attr("data-theme");

        element.click(function () {

            SetTheme(elementTheme.toLowerCase(), false);

            let content = {
                theme: elementTheme
            }

            $.ajax({
                type: "PUT",
                url: settingsUrl,
                dataType: "json",
                contentType: "application/json",
                data: JSON.stringify(content),
                error: function (data) {     // Always

                    // Error
                    if (data.status !== 200)
                        ShowAjaxErrorBox(data);
                }
            })
        });
    });
});