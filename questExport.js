function exportQuestsToJsonFile() {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheetQuests = ss.getSheetByName("Quests");

  const data = sheetQuests.getDataRange().getValues();
  const headers = data.shift().map(String);

  const get = (row, col) => {
    const i = headers.indexOf(col);
    return i >= 0 ? row[i] : null;
  };

  const quests = data
    .filter(row => str(row[0])) // skip empty rows
    .map(row => {

      // --- Required Actions (comma-separated parallel columns) ---
      const objectiveIds   = splitTokens(get(row, "objectiveIds"));
      const objectiveNames = splitTokens(get(row, "objectiveNames"));
      const objectiveQtys  = splitTokens(get(row, "objectiveQtys"));

      const requiredActions = objectiveIds.map((actionID, i) => ({
        actionID,
        name:        objectiveNames[i] || prettyActionName(actionID),
        requiredQty: parseInt(objectiveQtys[i], 10) || 1,
        currentQty:  0
      }));

      // --- Reward ---
      const rewardGold    = parseInt(str(get(row, "rewardGold")), 10) || 0;
      const rewardItemId  = str(get(row, "rewardItemId"));
      const rewardItemQty = parseInt(str(get(row, "rewardItemQty")), 10) || 1;
      const rewardItem    = rewardItemId ? { itemID: rewardItemId, quantity: rewardItemQty } : null;

      return {
        questId:         str(get(row, "questId")),
        questName:       str(get(row, "questName")),
        description:     str(get(row, "description")),
        questType:       str(get(row, "questType")),
        requiredActions,
        reward: { rewardGold, rewardItem },
        isCompleted:     false
      };
    });

  const jsonOutput = JSON.stringify(quests, null, 2);

  // Save to Google Drive under ExportedJSON/
  const folderName = "ExportedJSON";
  const folders = DriveApp.getFoldersByName(folderName);
  const folder = folders.hasNext() ? folders.next() : DriveApp.createFolder(folderName);

  const fileName = "Quests.json";
  const existing = folder.getFilesByName(fileName);
  while (existing.hasNext()) existing.next().setTrashed(true);

  folder.createFile(fileName, jsonOutput, MimeType.PLAIN_TEXT);
  Logger.log(`✅ Saved ${quests.length} quests to Drive/${folderName}/${fileName}`);
}

/* ---- Helpers ---- */

function str(v) {
  return (v == null) ? '' : String(v).trim();
}

// Split on commas, trim whitespace, drop blanks
function splitTokens(v) {
  if (v == null) return [];
  const s = String(v).trim();
  if (!s) return [];
  return s.split(',').map(x => x.trim()).filter(Boolean);
}

// Fallback: "Action_Stable_GroomMaple" -> "Groom Maple"
function prettyActionName(id) {
  if (!id) return "";
  let s = String(id).replace(/^Action[_-]/i, "");
  const parts = s.split(/[_-]/g).filter(Boolean);
  s = parts.length >= 2 ? parts[parts.length - 1] : parts.join(" ");
  s = s.replace(/([a-z])([A-Z])/g, "$1 $2");
  return s.replace(/\b\w+/g, w => w[0].toUpperCase() + w.slice(1).toLowerCase()).trim();
}
