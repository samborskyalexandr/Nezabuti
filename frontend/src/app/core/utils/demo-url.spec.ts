import { isAllowedDemoUrl } from './demo-url';

describe('isAllowedDemoUrl', () => {
  it('accepts site-relative and http(s) URLs', () => {
    expect(isAllowedDemoUrl('/m/ABC123XY')).toBeTrue();
    expect(isAllowedDemoUrl('https://nezabuti.com.ua/m/ABC')).toBeTrue();
    expect(isAllowedDemoUrl('http://localhost:8088/m/ABC')).toBeTrue();
  });

  it('rejects unsafe values', () => {
    expect(isAllowedDemoUrl('javascript:alert(1)')).toBeFalse();
    expect(isAllowedDemoUrl('//evil.example')).toBeFalse();
    expect(isAllowedDemoUrl('')).toBeFalse();
  });
});
