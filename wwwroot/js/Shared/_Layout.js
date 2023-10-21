"use strict";

$().ready(function () {
    const btn = $("#cookieConsent button[data-cookie-string]");

    btn.click(function () {
        document.cookie = btn.attr("data-cookie-string");
    });
});