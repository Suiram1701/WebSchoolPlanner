"use strict";

$().ready(function () {

    // cookie consent
    let cookieBtn = $("#cookieConsent button[data-cookie-string]");
    cookieBtn.click(function () {
        document.cookie = cookieBtn.attr("data-cookie-string");
    });

    // language switch
    $("a[id='languageSwitch']").each(function () {
        $(this).click(function () {
            let element = $(this);

            if (element.attr("class").includes("active"))     // Abort when current language
                return;

            const value = element.attr("data-culture");
            const cookieValue = "c=" + value + "|uic=" + value;    // The format for the parser
            let expires = new Date(new Date().getTime() + 31449600000);     // Expires in 1 Year - 1 Day
            document.cookie = ".AspNetCore.Culture=" + cookieValue + "; expires=" + expires.toUTCString() + "; path=/; SameSite=None; Secure";

            window.location.reload();
        });
    });
});