export type ApiResponse<T> = {
  success: boolean;
  message: string;
  data: T | null;
  errors: string[];
};

export type PagedResult<T> = {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
};
