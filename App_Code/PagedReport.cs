using System;
using System.Collections.Generic;
using System.Web;
using System.Data;

/// <summary>
/// Summary description for PagedReport
/// </summary>
public class PagedReport
{
    private DataTable _dt;
    private string _bodyText;
    private string _headerText;
    private string _pageText;
    private int _rowsPerPage;

	public PagedReport()
	{
		//
		// TODO: Add constructor logic here
		//
	}

    public PagedReport(DataTable dataTable)
    {
        Dt = dataTable;
    }

    public void BuildBodyText()
    {
        int counter = 0;

       

        foreach (DataRow row in _dt.Rows)
        {
            if (counter == 0)
            {

            }
            //BuildGridRow(row);
        }
    }

    #region properties
    
    protected DataTable Dt
    {
        get { return _dt; }
        set { _dt = value; }
    }

    protected int RowsPerPage
    {
        get { return _rowsPerPage; }
        set { _rowsPerPage = value; }
    }

    protected string HeaderText
    {
        get { return _headerText; }
        set { _headerText = value; }
    }

    protected string BodyText
    {
        get { return _bodyText; }
        set { _bodyText = value; }
    }


    public string PageText
    {
        get { return _pageText; }
        set { _pageText = value; }
    }

    #endregion
}
