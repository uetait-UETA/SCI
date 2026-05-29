<%@ Application Language="C#" %>
<script runat="server">
    void Application_Error(object sender, EventArgs e)
    {
        Server.ClearError();
        Response.Redirect("~/Login1.aspx");
    }
</script>
