﻿
DELETE WDE 
  FROM WF_ACTIVITY WFA
  JOIN WF_DECISION WDE ON (WFA.WFA_ID = WDE.WFA_ID)
 WHERE WFAD_ID = @WFAD_ID;

DELETE 
  FROM WF_ACTIVITY 
 WHERE WFAD_ID = @WFAD_ID;
