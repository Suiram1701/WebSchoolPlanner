"use strict";

function SetTheme(value, setCookie, setSvg) {

    if (setCookie) {
        // Set the saving cookie
        if (value !== "auto")
            Cookies.set(".AspNetCore.Theme", value, { expires: 364, path: "/", sameSite: "None", secure: true });
        else
            Cookies.remove(".AspNetCore.Theme", { path: "/" });
    }

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

    if (!setSvg)
        return;

    // Set the svg
    const svgContent = $("button[data-theme='" + value + "'] svg").prop('outerHTML')
    $("#theme-display").html(svgContent);
}

function LogAjaxError(data) {
    const errorMsg = "Error happened while request. Code: " + data.status + "; Error: " + data.responseText;
    console.error(errorMsg);
}

function ShowAjaxErrorBox(data) {

    // Determine the error message
    let errorMsg;
    const responseType = data.getResponseHeader('Content-Type');
    if (responseType === "application/json+problem")
        errorMsg = data.responseJSON.title;     // Json problem response
    else if (responseType === "application/json")
        errorMsg = data.responseJSON.error.message;     // Read from exception
    else
        errorMsg = "an unexpected error occurred.";

    ShowErrorBox(errorMsg);
}

function ShowErrorBox(error) {
    const errorBox = '<div class="alert alert-danger alert-dismissible d-flex" role="alert"><svg xmlns="http://www.w3.org/2000/svg" height="24" viewBox="0 -960 960 960" width="24"><path fill="var(--bs-alert-color)" d="M480-280q17 0 28.5-11.5T520-320q0-17-11.5-28.5T480-360q-17 0-28.5 11.5T440-320q0 17 11.5 28.5T480-280Zm-40-160h80v-240h-80v240Zm40 360q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q134 0 227-93t93-227q0-134-93-227t-227-93q-134 0-227 93t-93 227q0 134 93 227t227 93Zm0-320Z"/></svg><div class="mx-2">' + error + '</div><button class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>';
    $("#errorBoxPlaceHolder").html(errorBox);
}

$().ready(function () {

    // Init bootstrap tooltips
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(tooltip => {
        new bootstrap.Tooltip(tooltip)
    });

    // color theme setup
    const currentTheme = $("html").attr("data-bs-theme");
    if (currentTheme === "auto")
        SetTheme(currentTheme, false, false);

    // color theme switch
    $("button[id='colorThemeSwitch']").each(function () {
        let element = $(this);
        const themeValue = element.attr("data-theme");

        element.click(function () {
            SetTheme(themeValue, true, true);
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