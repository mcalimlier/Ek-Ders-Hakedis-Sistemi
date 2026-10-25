param([Parameter(Mandatory=$true)][string]$Workbook,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$excel=$null;$book=$null
$sourcePath=(Resolve-Path -LiteralPath $Workbook).Path
$before=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
try {
    $excel=New-Object -ComObject Excel.Application
    $excel.Visible=$false;$excel.DisplayAlerts=$false;$excel.AskToUpdateLinks=$false;$excel.AutomationSecurity=3
    $book=$excel.Workbooks.Open($sourcePath,0,$true)
    $inputSheet=$book.Worksheets.Item('VERİ GİRİŞİ');$payroll=$book.Worksheets.Item('Bordro')
    $excel.CalculateFullRebuild()
    $cases=@(@(0,0,0),@(60,0,0),@(27,0,0),@(18,0,0),@(1,1,10),@(117,0,1.005),@(7.5,0,0),@(7.51,0,0),@(0,1,0),@(0,117,0),@(200,10,12.345),@(400,100,0),@(0.005,0.005,0.005),@(117.123,2.567,1.005),@(148,0,0),@(150,0,0))
    $random=[Random]::new(20261025)
    for($i=0;$i -lt 34;$i++) {$cases+=,@([Math]::Round($random.NextDouble()*600,3),[Math]::Round($random.NextDouble()*100,3),[Math]::Round($random.NextDouble()*100,3))}
    $result=@()
    foreach($case in $cases) {
        $inputSheet.Range('P4').Value2=[double]$case[0];$inputSheet.Range('Q4').Value2=[double]$case[1];$inputSheet.Range('S4').Value2=[double]$case[2]
        $excel.CalculateFullRebuild()
        $row=@{DayHours=$case[0];DykHours=$case[1];Garnishment=$case[2]}
        $columns=@{DayGross=10;DykGross=9;PremiumDays=11;DayRate=13;DykRate=12;Gross=14;EmployeePremium=23;EmployerPremium=24;Accrual=16;IncomeBase=17;IncomeTax=20;StampTax=22;Deductions=26;Net=27}
        foreach($key in $columns.Keys) {$row[$key]=[double]$payroll.Cells.Item(7,$columns[$key]).Value2}
        $result+=$row
    }
    @{Method='Microsoft Excel CalculateFullRebuild; read-only source; no save';SourcePeriod='2026-09';Cases=$result} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Output -Encoding utf8
    Write-Output "$($result.Count) anonymous Excel scenarios recalculated."
} finally {
    if($book){$book.Close($false)}
    if($excel){$excel.Quit();[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($excel)}
    [GC]::Collect();[GC]::WaitForPendingFinalizers()
    if((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $before){throw 'Source workbook changed.'}
}
