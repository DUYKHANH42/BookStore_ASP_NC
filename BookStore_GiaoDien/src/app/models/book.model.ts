export interface Book {
  id: number;
  title: string;
  author: string;
  price: number;
  description: string;
  quantity: number;
  createdAt: string;
  updatedAt?: string;
  imageUrl: string;
  categoryId: number;
  isActive: boolean;
  sku?: string;
  subCategoryId?: number;
  
  // Flash Sale Fields
  discountPrice?: number;
  saleEndDate?: string;
  isFlashSale?: boolean;
  saleSoldCount?: number;
  saleStock?: number;
}
