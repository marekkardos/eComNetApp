import { User, Address } from '../models';

export const MOCK_USER: User = {
  email: 'test@test.com',
  displayName: 'Test User',
  token: 'mock-jwt-token-12345'
};

export const MOCK_ADDRESS: Address = {
  firstName: 'John',
  lastName: 'Doe',
  street: '123 Main Street',
  city: 'New York',
  state: 'NY',
  zipcode: '10001',
  country: 'USA'
};
