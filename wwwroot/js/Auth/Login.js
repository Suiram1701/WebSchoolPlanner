"use strict";

$().ready(function () {
    let formDiv = $("#form-div");
    if (!formDiv.hasClass("form-invalid"))     // Use only when input invalid
        return;

    $("input[class='form-control']").each(function () {
        $(this).on("input", function () {
            formDiv.removeClass("form-invalid");
        });
    });
});