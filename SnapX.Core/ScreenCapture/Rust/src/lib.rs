uniffi::include_scaffolding!("snapxrust");

use xcap::{
    image::{imageops, EncodableLayout, RgbaImage},
    Monitor, Window,
};

fn normalized(filename: &str) -> String {
    filename
        .replace("|", "")
        .replace("\\", "")
        .replace(":", "")
        .replace("/", "")
}
pub struct ImageData {
    image: Vec<u8>,
    width: u32,
    height: u32,
}
pub struct MonitorData {
    width: u32,
    height: u32,
    x: i32,
    y: i32,
    name: String,
}
pub struct WindowData {
    pub app_name: String,
    pub title: String,
    pub process_id: u32,
    pub hwnd: u32,
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
    pub is_minimized: bool,
    pub is_maximized: bool,
    pub is_focused: bool,
}

pub fn get_window_list() -> Vec<WindowData> {
    let windows = Window::all().unwrap();
    let mut list = Vec::new();

    for window in windows {
        let app_name = window.app_name().unwrap_or_default();
        let title = window.title().unwrap_or_default();
        let x = window.x().unwrap_or(0);
        let y = window.y().unwrap_or(0);
        let width = window.width().unwrap_or(0);
        let height = window.height().unwrap_or(0);
        let is_minimized = window.is_minimized().unwrap_or(false);
        let is_maximized = window.is_maximized().unwrap_or(false);
        let process_id = window.pid().unwrap_or(0);
        let is_focused = window.is_focused().unwrap_or(false);
        let hwnd = window.id().unwrap_or_default();

        list.push(WindowData {
            app_name,
            title,
            process_id,
            hwnd,
            x,
            y,
            width,
            height,
            is_minimized,
            is_maximized,
            is_focused,
        });
    }

    list
}

pub fn get_screen_dimensions(name: &str) -> MonitorData {
    let monitors = Monitor::all().unwrap();

    let monitor = monitors.iter().find(|m| m.name().unwrap() == name).unwrap(); // Will panic if no monitor is found, as the name is assumed valid
    MonitorData {
        width: monitor.width().expect("Failed to get monitor width"),
        height: monitor.height().expect("Failed to get monitor height"),
        x: monitor.x().expect("Failed to get monitor x"),
        y: monitor.y().expect("Failed to get monitor y"),
        name: monitor.name().unwrap(),
    }
}
pub fn get_working_area() -> MonitorData {
    let monitors = Monitor::all().unwrap();

    let width: u32 = monitors
        .iter()
        .map(|m| m.width().expect("Failed to get monitor width"))
        .sum();

    let height: u32 = monitors
        .iter()
        .map(|m| m.height().expect("Failed to get monitor width"))
        .max()
        .unwrap_or(0);
    let name = monitors
        .iter()
        .map(|m| m.name().unwrap())
        .collect::<Vec<String>>()
        .join(", ");

    MonitorData {
        width,
        height,
        x: 0,
        y: 0,
        name,
    }
}

pub fn get_monitor(x: u32, y: u32) -> MonitorData {
    let monitor = Monitor::from_point(x as i32, y as i32).unwrap();
    MonitorData {
        width: monitor.width().expect("Failed to get monitor width"),
        height: monitor.height().expect("Failed to get monitor height"),
        x: x as i32,
        y: y as i32,
        name: monitor.name().unwrap(),
    }
}
pub fn get_primary_monitor() -> MonitorData {
    let monitors = Monitor::all().unwrap();

    let monitor = monitors.iter().find(|m| m.is_primary().unwrap()).unwrap();
    MonitorData {
        width: monitor.width().expect("Failed to get monitor width"),
        height: monitor.height().expect("Failed to get monitor height"),
        x: monitor.x().expect("Failed to get monitor x"),
        y: monitor.y().expect("Failed to get monitor y"),
        name: monitor.name().unwrap(),
    }
}

pub fn capture_monitor(name: &str) -> ImageData {
    let monitors = Monitor::all().unwrap();

    let monitor = monitors.iter().find(|m| m.name().unwrap() == name).unwrap();

    let image = monitor.capture_image().unwrap();
    let image_bytes = image.as_bytes().to_vec();
    let width = image.width();
    let height = image.height();

    ImageData {
        image: image_bytes,
        width,
        height,
    }
}
pub fn capture_fullscreen() -> ImageData {
    let monitors = Monitor::all().unwrap();

    let total_width = monitors
        .iter()
        .map(|m| {
            (m.x().expect("Failed to get monitor x") as u32
                + m.width().expect("Failed to get monitor width"))
        })
        .max()
        .unwrap();

    let total_height = monitors
        .iter()
        .map(|m| {
            (m.y().expect("Failed to get monitor y") as u32
                + m.height().expect("Failed to get monitor height"))
        })
        .max()
        .unwrap();

    let mut combined_image = RgbaImage::new(total_width, total_height);

    for monitor in monitors {
        let monitor_image = monitor.capture_image().unwrap();

        let monitor_x = monitor.x().expect("Failed to get monitor x") as u32;
        let monitor_y = monitor.y().expect("Failed to get monitor y") as u32;
        let monitor_width = monitor_image.width();
        let monitor_height = monitor_image.height();

        for y in 0..monitor_height {
            for x in 0..monitor_width {
                let pixel = monitor_image.get_pixel(x, y);
                combined_image.put_pixel(x + monitor_x, y + monitor_y, *pixel); // Place the pixel at the correct position
            }
        }
    }

    let width = combined_image.width();
    let height = combined_image.height();
    let image_bytes = combined_image.as_bytes().to_vec();

    ImageData {
        image: image_bytes,
        width,
        height,
    }
}

pub fn capture_window_by_handle(handle: u64) -> ImageData {
    let windows = Window::all().unwrap();
    let window = windows
        .iter()
        .find(|w| w.id().unwrap() as u64 == handle)
        .expect("Window handle not found");

    let image = window.capture_image().unwrap();
    let image_bytes = image.as_bytes().to_vec();
    let width = image.width();
    let height = image.height();

    ImageData {
        image: image_bytes,
        width,
        height,
    }
}

pub fn capture_window(x: u32, y: u32) -> ImageData {
    let windows = Window::all().unwrap();

    let mut sorted_windows: Vec<_> = windows.iter().collect();
    sorted_windows.sort_by(|a, b| b.z().unwrap().cmp(&a.z().unwrap()));
    println!("Total windows: {}", sorted_windows.len());
    for (index, w) in sorted_windows.iter().enumerate() {
        println!(
            "[{}] {} {} {} {}",
            index,
            w.title().unwrap(),
            w.app_name().unwrap(),
            w.pid().unwrap(),
            w.z().unwrap()
        );
    }

    let window = sorted_windows
        .iter()
        .find(|w| {
            let win_x = w.x().unwrap() as u32;
            let win_y = w.y().unwrap() as u32;
            let win_width = w.width().unwrap();
            let win_height = w.height().unwrap();

            x >= win_x && x <= win_x + win_width && y >= win_y && y <= win_y + win_height
        })
        .unwrap();

    capture_window_by_handle(window.id().unwrap() as u64)
}

pub fn capture_rect(x: u32, y: u32, width: u32, height: u32) -> ImageData {
    let monitor = Monitor::from_point(x as i32, y as i32).unwrap();
    let mut full_image = monitor.capture_image().unwrap();
    let image = imageops::crop(&mut full_image, x, y, width, height).to_image();

    let width = image.width();
    let height = image.height();
    let image_bytes = image.as_bytes().to_vec();

    ImageData {
        image: image_bytes,
        width,
        height,
    }
}
