"use strict";

function SetTheme(value, loadSvg) {

    // Set the saving cookie
    if (value !== "auto")
        Cookies.set(".AspNetCore.Theme", value, { expires: 364, path: "/", sameSite: "None", secure: true });
    else
        Cookies.remove(".AspNetCore.Theme", { path: "/" });

    // Disable all non-active btns
    $("button[data-theme]").each(function () {
        let element = $(this);
        element.removeClass("active")
        element.attr("aria-selected", false);
    });

    // Set the active btn
    let btnElement = $("button[data-theme='" + value + "']");
    btnElement.attr("aria-pressed", true);
    btnElement.addClass("active");

    let attrValue = value;
    if (value === "auto") {     // Set the auto value
        if (window.matchMedia('(prefers-color-scheme: dark)').matches)
            attrValue = "dark";
        else
            attrValue = "white";
    }

    $("html").attr("data-bs-theme", attrValue);

    if (!loadSvg)
        return;

    // Set the svg
    const svgContent = $("button[data-theme='" + value + "'] svg").prop('outerHTML')
    $("#theme-display").html(svgContent);
};

$().ready(function () {

    // color theme setup
    const currentTheme = $("html").attr("data-bs-theme");
    SetTheme(currentTheme, false);

    // color theme switch
    $("button[data-theme]").each(function () {
        let element = $(this);
        const themeValue = element.attr("data-theme");

        element.click(function () {
            SetTheme(themeValue, true)
        });
    });

    // cookie consent
    let cookieBtn = $("#cookieConsent button[data-cookie-string]");
    cookieBtn.click(function () {
        document.cookie = cookieBtn.attr("data-cookie-string");
    });

    // language switch
    $("a[id='languageSwitch']").each(function () {
        $(this).click(function () {
            let element = $(this);

            if (element.hasClass("active"))     // Abort when current language
                return;

            const value = element.attr("data-culture");
            const cookieValue = "c=" + value + "|uic=" + value;    // The format for the parser
            Cookies.set(".AspNetCore.Culture", cookieValue, { expires: 364, path: "/", sameSite: "None", secure: true });


            window.location.reload();
        });
    });
});