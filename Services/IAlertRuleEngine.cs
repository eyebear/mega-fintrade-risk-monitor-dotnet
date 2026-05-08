namespace MegaFintradeRiskMonitor.Services;

public interface IAlertRuleEngine
{
    AlertRuleEvaluationResult Evaluate(AlertRuleEvaluationRequest request);
}