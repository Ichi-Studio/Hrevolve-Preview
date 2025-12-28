/**
 * 侧边栏性能测试脚本
 * 在浏览器控制台运行此脚本来测试优化效果
 */

// 测试菜单展开性能
function testMenuPerformance() {
  console.log('🚀 开始测试侧边栏性能...\n');
  
  // 1. 测试菜单渲染时间
  console.time('⏱️  菜单初始渲染');
  const menuItems = document.querySelectorAll('.el-menu-item, .el-sub-menu');
  console.timeEnd('⏱️  菜单初始渲染');
  console.log(`📊 菜单项数量: ${menuItems.length}\n`);
  
  // 2. 测试子菜单展开性能
  const subMenus = document.querySelectorAll('.el-sub-menu');
  if (subMenus.length > 0) {
    console.log('🔽 测试子菜单展开性能...');
    
    subMenus.forEach((menu, index) => {
      const title = menu.querySelector('.el-sub-menu__title');
      if (title) {
        console.time(`  子菜单 ${index + 1} 展开`);
        title.click();
        setTimeout(() => {
          console.timeEnd(`  子菜单 ${index + 1} 展开`);
        }, 100);
      }
    });
  }
  
  // 3. 测试内存占用
  setTimeout(() => {
    if (performance.memory) {
      const memory = performance.memory;
      console.log('\n💾 内存占用:');
      console.log(`  已用: ${(memory.usedJSHeapSize / 1048576).toFixed(2)} MB`);
      console.log(`  总计: ${(memory.totalJSHeapSize / 1048576).toFixed(2)} MB`);
      console.log(`  限制: ${(memory.jsHeapSizeLimit / 1048576).toFixed(2)} MB`);
    }
    
    // 4. 测试 FPS
    console.log('\n🎬 开始测试动画 FPS (持续 3 秒)...');
    testFPS();
  }, 1000);
}

// 测试 FPS
function testFPS() {
  let frames = 0;
  let lastTime = performance.now();
  const duration = 3000; // 测试 3 秒
  const startTime = lastTime;
  
  function countFrame() {
    frames++;
    const currentTime = performance.now();
    
    if (currentTime - startTime < duration) {
      requestAnimationFrame(countFrame);
    } else {
      const elapsed = (currentTime - lastTime) / 1000;
      const fps = Math.round(frames / elapsed);
      console.log(`📈 平均 FPS: ${fps}`);
      
      if (fps >= 55) {
        console.log('✅ 性能优秀！');
      } else if (fps >= 45) {
        console.log('⚠️  性能良好，但有优化空间');
      } else {
        console.log('❌ 性能较差，需要优化');
      }
      
      console.log('\n✨ 测试完成！');
    }
  }
  
  requestAnimationFrame(countFrame);
}

// 测试响应式性能
function testReactivityPerformance() {
  console.log('\n🔄 测试响应式性能...');
  
  const iterations = 1000;
  console.time(`  ${iterations} 次菜单状态切换`);
  
  for (let i = 0; i < iterations; i++) {
    // 模拟状态变化
    const event = new Event('click');
    const firstMenu = document.querySelector('.el-sub-menu__title');
    if (firstMenu) {
      firstMenu.dispatchEvent(event);
    }
  }
  
  console.timeEnd(`  ${iterations} 次菜单状态切换`);
}

// 导出测试函数
window.testMenuPerformance = testMenuPerformance;
window.testReactivityPerformance = testReactivityPerformance;

console.log('📝 性能测试工具已加载！');
console.log('运行以下命令开始测试:');
console.log('  testMenuPerformance()      - 测试菜单性能');
console.log('  testReactivityPerformance() - 测试响应式性能');
