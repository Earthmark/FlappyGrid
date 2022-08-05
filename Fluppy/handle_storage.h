#pragma once

#include "absl/container/flat_hash_map.h"

template<typename T>
class handle_storage
{
public:
  using handle_type = uint32_t;

  handle_storage()
  {
    buffer_.reserve(1024);
  }

  handle_type allocate(T&& data)
  {
    uint32_t handle = next_free_++;
    buffer_.emplace(handle, std::move(data));
    return handle;
  }

  void free(handle_type handle)
  {
    buffer_.erase(handle);
  }

  T* get(handle_type handle)
  {
    if (auto found = buffer_.find(handle); found != buffer_.end())
    {
      return &found->second;
    }
    return nullptr;
  }

private:
  std::atomic<handle_type> next_free_{ 0 };
  absl::flat_hash_map<handle_type, T> buffer_;
};
