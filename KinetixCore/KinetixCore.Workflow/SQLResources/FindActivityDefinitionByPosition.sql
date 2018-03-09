﻿SELECT 
	WAD.WFAD_ID AS WfadId,
	WAD.NAME AS Name,
	WAD.LEVEL AS Level,
	WAD.WFMD_CODE AS WfmdCode,
	WAD.WFWD_ID AS WfwdId
FROM 
	WF_ACTIVITY_DEFINITION WAD
WHERE WAD.WFWD_ID = @WFWD_ID
	  AND WAD.LEVEL = @LEVEL;