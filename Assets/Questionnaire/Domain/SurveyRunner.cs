using System;
using System.Collections.Generic;

namespace Questionnaire
{
/// <summary>Orchestrates a survey walk-through. Headless-testable (no Unity types).</summary>
public sealed class SurveyRunner : IDisposable
{
    private readonly Survey _survey;
    private readonly Func<long> _nowMs;
    private readonly List<Item> _items;
    private ISurveyLogger _logger;
    private int _index;
    private long _itemStartMs;
    private IQuestionWidget _widget;
    private bool _closed;

    public SurveyRunner(Survey survey, ISurveyLogger logger, Func<long> nowMs)
    {
        _survey = survey ?? throw new ArgumentNullException(nameof(survey));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _nowMs = nowMs ?? throw new ArgumentNullException(nameof(nowMs));
        _items = new List<Item>();
        foreach (var sec in survey.sections ?? new List<Section>())
        foreach (var it in sec.items ?? new List<Item>())
            _items.Add(it);
        _itemStartMs = _nowMs();
    }

    public IReadOnlyList<Item> Items => _items.AsReadOnly();

    public bool IsComplete => _index >= _items.Count;

    public Item CurrentItem => IsComplete ? null : _items[_index];

    public bool CanSubmit => _widget != null && _widget.IsComplete;

    public void AttachWidget(IQuestionWidget widget) => _widget = widget;

    public bool Submit()
    {
        if (!CanSubmit)
            return false;

        var item = CurrentItem;
        long now = _nowMs();
        var response = new SurveyResponse(
            surveyId: _survey.id,
            surveyVersion: _survey.version,
            sectionId: FindSectionId(item),
            itemId: item.id,
            type: item.type,
            value: _widget.ReadValue(),
            submittedAtEpochMs: now,
            durationSec: (float)(now - _itemStartMs) / 1000.0f);

        _logger.Log(in response);

        _index++;
        if (!IsComplete)
        {
            _itemStartMs = _nowMs();
        }
        else
        {
            _logger.Close();
            _closed = true;
        }

        _widget = null;
        return true;
    }

    private string FindSectionId(Item item)
    {
        if (item == null || _survey.sections == null) return string.Empty;
        foreach (var sec in _survey.sections)
        foreach (var it in sec.items ?? new List<Item>())
            if (it.id == item.id)
                return sec.id;
        return string.Empty;
    }

    public void Close()
    {
        if (!_closed)
        {
            _logger?.Close();
            _closed = true;
        }
    }

    public void Dispose() => Close();
}
}
