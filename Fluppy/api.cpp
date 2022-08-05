#include "api.h"

#include "job_processor.h"

job_processor processor;

extern "C" __declspec(dllexport) exec_handle execute(buffer_span span)
{
  return processor.execute(span.to_span());
}

extern "C" __declspec(dllexport) execution_result_span execution_results()
{
  return execution_result_span::make(processor.consume_results());
}
