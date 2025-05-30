use std::process::Command;
use camino::Utf8Path;
use uniffi_bindgen::{bindings::{TargetLanguage}, generate_bindings};

fn main() {
        let out_dir = "./bindings/";
        let udl_file = "./src/snapxrust.udl";
        let s = "snapxrust";
        let path = Utf8Path::new("./Cargo.toml");
        uniffi_build::generate_scaffolding(udl_file).unwrap();
        generate_bindings(udl_file.into(),
        Some(path),
                vec![TargetLanguage::Python],
                Some(out_dir.into()),
                None,
                s.into(), true).unwrap();
            if Command::new("uniffi-bindgen-cs").arg("--version").output().is_err() {
        println!("Installing uniffi-bindgen-cs...");
        Command::new("cargo")
            .arg("install")
            .arg("uniffi-bindgen-cs")
            .arg("--git")
            .arg("https://github.com/NordSecurity/uniffi-bindgen-cs")
            .arg("--tag")
            .arg("v0.8.4+v0.25.0")
            .status().expect("Failed to install uniffi-bindgen-cs");
        }
        Command::new("uniffi-bindgen-cs").arg("--out-dir").arg(out_dir).arg(udl_file).arg("--config").arg("Cargo.toml").output().expect("Failed when generating C# bindings");
}
