"use strict";

function TogglePasswdVisibility() {
    let visibilityElement = $("#passwdVisibility");
    let passwdInput = $("#passwdInput");
    if (passwdInput.attr("type") === "password") {
        visibilityElement.attr("src", "/img/visibility.svg");
        passwdInput.attr("type", "text");
    }
    else {
        visibilityElement.attr("src", "/img/visibility_off.svg");
        passwdInput.attr("type", "password");
    }
}

$().ready(function () {
    let formDiv = $("#form-div");
    if (!formDiv.hasClass("form-invalid"))     // Use only when input invalid
        return;

    // Remove form invalid after a new input
    $("input[class='form-control']").each(function () {
        $(this).on("input", function () {
            formDiv.removeClass("form-invalid");
        });
    });
});