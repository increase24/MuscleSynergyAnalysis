clear
clc

% EMG import
EMG=importdata('./EMG.txt');
%EMG=EMG.data;
EMG=EMG(:,5:8);

% ��ͨ��-����ͷ��
tri=EMG(1:90000,4);

% 20-450hz ������˹��ͨ�˲�
fs = 1000;
[b,a] = butter(4,[20*2/fs 450*2/fs]);
y = filter(b,a,tri);

%ȫ������
fw = abs(y);
% 2hz ������˹��ͨ�˲� ģ�⼡���ͨ�˲�������
[d1,c1] = butter(4,5*2/fs,'low');
z = filter(d1,c1,fw);

%��һ��
m=max(z); % mΪ������mvc
z=z/m;


% �񾭼���̶� u
d = 10; % 10ms ʱ���ӳ�
c1 = 0.5;
c2 = 0.5;
beta1 = c1+c2;
beta2 = c1*c2;
alpha = 1+beta1+beta2;

for ii  = 1: d
    u(ii) = 0;
end
    
for ii = 1:length(z)
    u(ii+d) = alpha*z(ii) - beta1*u(ii+d-1) - beta2*u(ii+d-2); 
end

% ���⼤��̶� a
A = -1.5; % -3 to 0
a = (exp(A*u) - 1) ./ (exp(A) -1);  %���⼤��̶�

