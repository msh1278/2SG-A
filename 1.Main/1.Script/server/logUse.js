const XLSX = require('xlsx');
const fs = require('fs');

// 새로운 로그 데이터를 추가하는 함수
function addLogToExcel(logData) {
  /*
  try {
    // 기존 Excel 파일을 읽어옵니다.
    let workbook;
    const filePath = 'log.xlsx';
    
    if (fs.existsSync(filePath)) {
      try {
        // 이미 파일이 존재하면 열기
        workbook = XLSX.readFile(filePath);
      } catch (err) {
        console.error('Error reading log file:', err.message);
        // 파일 읽기 실패 시 새로운 워크북 생성
        workbook = XLSX.utils.book_new();
      }
    } else {
      // 파일이 없으면 새로운 워크북 생성
      workbook = XLSX.utils.book_new();
    }

    // 워크시트를 가져오거나 새로 생성합니다.
    let sheetName = 'Logs';
    let worksheet;
    
    if (workbook.Sheets[sheetName]) {
      worksheet = workbook.Sheets[sheetName];
    } else {
      // 워크시트가 없으면 새로 생성하고, 헤더 추가
      worksheet = XLSX.utils.aoa_to_sheet([['Timestamp', 'Log']]); 
      workbook.Sheets[sheetName] = worksheet;
      workbook.SheetNames.push(sheetName); // 시트 이름을 Workbook에 추가
    }

    // 로그 데이터를 시트에 추가
    const timestamp = new Date().toISOString();
    const newRow = [timestamp, logData];
    
    // 마지막 행의 인덱스를 찾고, 새 데이터를 추가합니다.
    const lastRow = XLSX.utils.sheet_to_json(worksheet, { header: 1 }).length;
    XLSX.utils.sheet_add_aoa(worksheet, [newRow], { origin: -1 });

    // 변경된 워크북을 다시 파일로 저장합니다.
    try {
      XLSX.writeFileSync(workbook, filePath);
    } catch (writeErr) {
      console.error('Error writing to log file:', writeErr.message);
      // 파일 쓰기 실패 시 로그만 출력하고 계속 진행
    }
  } catch (e) {
    // 로깅 프로세스에서 오류가 발생해도 애플리케이션은 계속 실행
    console.error('Logging error (non-fatal):', e.message);
  }
  */
  // 콘솔에도 로그 출력
  console.log(logData);
}

module.exports = addLogToExcel;