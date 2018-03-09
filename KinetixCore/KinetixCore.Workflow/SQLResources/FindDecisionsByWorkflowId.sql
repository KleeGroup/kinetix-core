﻿SELECT 
	WDE.*
FROM 
	WF_DECISION WDE
	JOIN WF_ACTIVITY WAC ON (WDE.WFA_ID = WAC.WFA_ID)
WHERE 
	WAC.WFW_ID = @WFW_ID
;