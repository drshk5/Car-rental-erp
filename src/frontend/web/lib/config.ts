export const appConfig = {
  name: "Car Rental ERP",
  apiBaseUrl: process.env.INTERNAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5001/api/v1",
  appUrl: process.env.NEXT_PUBLIC_APP_URL ?? "http://localhost:3000",
};
