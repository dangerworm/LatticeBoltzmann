function x = plot_velocity_vectors() 

load result.mat

n = 3;
xr=(0:1:Lx-1)*dx;
yr=(0:1:Ly-1)*dy;
[xc,yc] = ndgrid(xr,yr);

quiver(xc(1:n:Lx, 1:1:Ly),yc(1:n:Lx, 1:1:Ly),u(1:n:Lx, 1:1:Ly),v(1:n:Lx, 1:1:Ly), 2);

hold on

% The cylinder
x0 = 10;
y0 = 2.75;
r = 0.25;
sita=0:pi/20:2*pi;
plot(x0+r'*cos(sita), y0+r'*sin(sita),'LineWidth', 2);

% Set axes
xlabel('x(m)'), ylabel('y(m)')
axis([9 15 1.5 4.2])
pbaspect([2.2 1 1])

title ('Flow around cylinder: velocity vectors')
legend 'Lattice Boltzmann solution' 
hold off
