param([Parameter(Mandatory=$true)][string]$Workbook,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$excel=$null;$book=$null
try {
    # Separate hidden instance; original is read-only and always closed without saving.
    $excel=New-Object -ComObject Excel.Application
    $excel.Visible=$false;$excel.DisplayAlerts=$false;$excel.AskToUpdateLinks=$false;$excel.AutomationSecurity=3
    $book=$excel.Workbooks.Open((Resolve-Path -LiteralPath $Workbook).Path,0,$true)
    $inputSheet=$book.Worksheets.Item('VERİ GİRİŞİ');$payroll=$book.Worksheets.Item('Bordro (Çıktı-1)')
    $baseline=@()
    for($r=4;$r -le 13;$r++) {
        $baseline+=@{DayHours=$inputSheet.Cells.Item($r,16).Value2;DykHours=$inputSheet.Cells.Item($r,17).Value2;Garnishment=$inputSheet.Cells.Item($r,19).Value2;Gross=$payroll.Cells.Item($r+3,14).Value2;Net=$payroll.Cells.Item($r+3,27).Value2}
    }
    $cases=@(@(0,0,0),@(117,0,0),@(108,0,0),@(45,0,0),@(63,0,0),@(54,0,0),@(1,1,10),@(117,0,1.005),@(7.5,0,0),@(7.51,0,0),@(0,1,0),@(0,117,0),@(200,10,12.345),@(400,100,0),@(0.005,0.005,0.005),@(117.123,2.567,1.005))
    $rng=[Random]::new(20261023)
    for($i=0;$i -lt 34;$i++) {$cases+=,@([Math]::Round($rng.NextDouble()*600,3),[Math]::Round($rng.NextDouble()*100,3),[Math]::Round($rng.NextDouble()*100,3))}
    $result=@()
    foreach($case in $cases) {
        $inputSheet.Range('P4').Value2=[double]$case[0];$inputSheet.Range('Q4').Value2=[double]$case[1];$inputSheet.Range('S4').Value2=[double]$case[2]
        $excel.CalculateFullRebuild()
        $row=@{DayHours=$case[0];DykHours=$case[1];Garnishment=$case[2]}
        $columns=@{DayGross=10;DykGross=9;PremiumDays=11;DayRate=13;DykRate=12;Gross=14;EmployeePremium=23;EmployerPremium=24;Accrual=16;IncomeBase=17;IncomeTax=20;StampTax=22;Deductions=26;Net=27}
        foreach($key in $columns.Keys) {$row[$key]=$payroll.Cells.Item(7,$columns[$key]).Value2}
        $result+=$row
    }
    @{Source=(Split-Path -Leaf $Workbook);Method='Microsoft Excel CalculateFullRebuild; read-only source; no save';Cases=$result;Baseline=$baseline} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Output -Encoding utf8
    Write-Output "$($result.Count) Excel scenarios recalculated. Original workbook unchanged."
} finally {
    if($book){$book.Close($false)}
    if($excel){$excel.Quit();[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($excel)}
    [GC]::Collect();[GC]::WaitForPendingFinalizers()
}
