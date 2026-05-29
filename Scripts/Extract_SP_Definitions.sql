-- ============================================================
-- Extract all stored procedure definitions used by SCIv2Brch
-- Run on SMM database (SMM_DFC / SMM_LAT)
-- ============================================================
SELECT
    sp.name                                          AS SP_Name,
    sm.definition                                    AS SP_Definition
FROM sys.objects sp
INNER JOIN sys.sql_modules sm ON sp.object_id = sm.object_id
WHERE sp.type = 'P'
  AND sp.name IN (
      'SISINV_GET_ACCESSTYPE_PRC',
      'Item_Bin_Gen_Prc',
      'la_update_transfer_errors',
      'S_Cia_iPrinters_ByUserAndSapCia',
      'SMM_DELETE_DRAFT_TRANSFER',
      'SMM_GET_LOGIN_WHS_TYPE_PRC',
      'SMM_UACTIONS_CNT',
      'SMM_UPLOAD_MINMAX',
      'Smm_Get_DispCompleted_Prc',
      'Smm_populate_discrep_odrf',
      'Smm_populate_whs_transfers_Batch',
      'Smm_ValDispatching_Order_Prc',
      'Smm_ValReciving_Order_Prc',
      'smm_insTransferXexcel_stg',
      'smm_insert_Transdiscrep_audit_odrf',
      'smm_login_validations',
      'smm_populate_Smm_Draft',
      'smm_populate_TransXexcel_odrf',
      'SP_INVENTORY_SAP_Location_dfa',
      'sp_BinesDEspachados',
      'sp_corteped_todoseti',
      'sp_ordenesProblem',
      'update_discrep_drf1',
      'update_TransXsap_drf1',
      'delete_TransXsap_drf1',
      'reseq_TransXsap_drf1',
      'delete_TransXsap',
      'submit_DraftXsap',
      'SMM_UpdateDeliveryItemNumber_PRC',
      'SMM_UpdateDeliveryItemCode_PRC',
      'Whs_DelBinsExcel_stg',
      'Whs_UploadBinsExcel_stg',
      'Whs_Item_Bin_Gen_FromStg'
  )
ORDER BY sp.name;
