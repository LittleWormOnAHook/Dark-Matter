$path = "a:\Survival Pioneer\Assets\Animations\Third Person Character\Animator\Third Person Animator Controller GKC.controller"
$content = Get-Content $path -Raw

$states = @{}
[regex]::Matches($content, '(?ms)--- !u!1102 &(-?\d+)\r?\nAnimatorState:.*?  m_Name: (.+?)\r?\n') | ForEach-Object {
    $states[$_.Groups[1].Value] = $_.Groups[2].Value
}

$results = @()
[regex]::Matches($content, '(?ms)--- !u!1101 &\d+\r?\nAnimatorStateTransition:.*?  m_Conditions:(.*?)  m_DstState') | ForEach-Object {
    $block = $_.Groups[1].Value
    if ($block -notmatch 'Action ID') { return }
    $hasActive = $block -match 'Action Active'
    $idMatch = [regex]::Match($block, 'm_ConditionEvent: Action ID\r?\n    m_EventTreshold: (-?\d+)')
    if (-not $idMatch.Success) { return }
    $dstMatch = [regex]::Match($_.Value, 'm_DstState: \{fileID: (-?\d+)')
    if (-not $dstMatch.Success) { return }
    $id = [int]$idMatch.Groups[1].Value
    $dst = $dstMatch.Groups[1].Value
    $name = if ($states.ContainsKey($dst)) { $states[$dst] } else { '?' }
    $results += [pscustomobject]@{ HasActive=$hasActive; Id=$id; State=$name }
}

$keywords = 'Punch','Sword','Axe','Block','Dead','Hit','Keep','Special','Charge','Melee','Damage','Power'
$results | Where-Object { $_.State -match ($keywords -join '|') } | Sort-Object Id -Unique | Format-Table -AutoSize
