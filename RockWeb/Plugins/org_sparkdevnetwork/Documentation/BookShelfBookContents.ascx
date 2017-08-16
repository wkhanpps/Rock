<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BookShelfBookContents.ascx.cs" Inherits="RockWeb.Plugins.org_sparkdevnetwork.Documentation.BookShelfBookContents" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Literal ID="lMessage" runat="server"></asp:Literal>
        <asp:Panel ID="pnlDetails" runat="server">       
            <div>
            
                <div id="divBookVersionSelector" runat="server" class="bookversion clearfix">
                    <div class="bookversion-heading row ">
                        <div class="col-xs-12 col-sm-6 col-md-4 pull-right">
                            <div class="bookversion-heading-tab">
                                Current Version: <asp:Literal ID="lBookVersion" runat="server"></asp:Literal> <i class="fa fa-chevron-down"></i>
                            </div>
                        </div>
                    </div>
                    
                    <div class="bookversion-details" style="display: none;">
                        <div class="row">
                            <div class="col-xs-12 col-sm-6 col-md-4 col-md-offset-4">
                                <Rock:RockDropDownList ID="ddlVersionsSince" CssClass="js-bookversion-versionsinceselector" Label="Show Changes Since" runat="server"></Rock:RockDropDownList>
                            </div>
                            <div class="col-xs-12 col-sm-6 col-md-4">
                                <Rock:RockDropDownList ID="ddlBookVersions" Label="Current Version" AutoPostBack="True" OnSelectedIndexChanged="ddlBookVersions_SelectedIndexChanged" runat="server"></Rock:RockDropDownList>
                            </div>
                        </div>
                    </div>

                </div>
                <div class="book-header jumbotron clearfix">
                    <div class="img-content book-cover margin-r-lg">
                        <asp:Image ID="imgCover" runat="server" />
                    </div>

                    <h1><asp:Literal ID="lTitle" runat="server"></asp:Literal></h1>
                    <p><asp:Literal ID="lSubtitle" runat="server"></asp:Literal></p>
                </div>

                <div class="row">
                    <div class="col-md-4">
                        <div class="book-sidebar">
                            <div class="book-toc margin-r-lg">
                                <p>Table of Contents</p>
                                <ul class="list-unstyled">
                                    <asp:Literal ID="lToc" runat="server"></asp:Literal>
                                </ul>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-8 book-contents">
                        <div class="report-issue" id="divReportIssue" runat="server"><i class="fa fa-bullhorn"></i> Improve</div>
                        
                        <asp:Literal ID="lUpdateSummaries" runat="server"></asp:Literal>
                        
                        <asp:Literal ID="lContent" runat="server"></asp:Literal>
                    </div>
                </div>
            </div>
</asp:Panel>       

        <asp:Literal ID="lVersionScript" runat="server"></asp:Literal>

        <script>
            $(document).ready(function () {
                $(".bookversion-heading").on("click", function () {
                    $(this).siblings(".bookversion-details").slideToggle();
                    $(this).find('i.fa').toggleClass("fa-chevron-down fa-chevron-up");
                });

                $(".book-sidebar").sticky({ topSpacing: 20 });
                //$(".report-issue").sticky({ topSpacing: 20 });

                $(".report-issue").mouseenter(
                    function () {
                        $(".report-issue").stop().animate({
                            right: "-4px",
                            opacity: 1
                        }, 500, function() {
                            // Animation complete.
                        });
                });

                $(".report-issue").mouseleave(
                    function () {
                        $(".report-issue").stop().animate({
                            right: "-80px",
                            opacity: .5
                        }, 500, function () {
                            // Animation complete.
                        });
                });

                $(function () {
                    $("a.book-image").fluidbox();
                });

                // reusable function to get query parameter
                function getParameterByName(name) {
                    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
                    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
                        results = regex.exec(location.search);
                    return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
                }

                var scrollPercent = getParameterByName('ScrollPercentage');
                if (scrollPercent != '') {
                    var bheight = $(document).height();
                    var percent = scrollPercent / 100;
                    var hpercent = bheight * percent;
                    $('html, body').animate({ scrollTop: hpercent }, 1000);
                }

                // highlight changes
                $(".js-bookversion-versionsinceselector").on('change', function () {
                    
                    var selectedVersion = $(this).val();

                    if (selectedVersion == '') {
                        // clear all update highlights and summaries
                        $("div[data-type='update-summary']").slideUp();
                        $("[data-update-tag]").removeClass('show-update');
                    } else  {
                        var showVersion = true;

                        bookVersionOrder.forEach(function (version) {
                            console.log(version);

                            if (version == selectedVersion) {
                                showVersion = false;
                            }

                            if (showVersion) {
                                $("[data-type='update-summary'][data-update-tag='" + version + "']").slideDown();
                                $("[data-update-tag*='" + version + "']").addClass('show-update');
                            } else {
                                $("[data-type='update-summary'][data-update-tag='" + version + "']").slideUp();
                                $("[data-update-tag*='" + version + "']").removeClass('show-update');
                            }
                        });

                    }; 
                });

            });
        </script>

    </ContentTemplate>
</asp:UpdatePanel>
