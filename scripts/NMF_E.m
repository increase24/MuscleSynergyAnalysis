function [W, H] = nmf(V, K, MAXITER)
%Euclidean distance
F = size(V,1);
T = size(V,2);
  
rand('seed',0)
W = 1+rand(F, K);
% W = W./repmat(sum(W),F,1);
H = 1+rand(K, T);
  
ONES = ones(F,T);
  
for i=1:MAXITER
    H = H .* (W'*V)./(W'*W*H+eps) ;
    W = W .* (V*H')./(W*H*H'+eps);
end