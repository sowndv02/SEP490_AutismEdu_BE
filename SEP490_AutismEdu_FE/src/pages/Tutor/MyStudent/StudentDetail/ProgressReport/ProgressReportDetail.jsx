import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import CloseIcon from '@mui/icons-material/Close';
import DoneIcon from '@mui/icons-material/Done';
import EditNoteIcon from '@mui/icons-material/EditNote';
import ListAltIcon from '@mui/icons-material/ListAlt';
import { Box, List, ListItem, ListItemIcon, ListItemText, Modal, Stack, Typography } from '@mui/material';
function ProgressReportDetail({ open, setOpen, selectedItem }) {
    const handleClose = () => setOpen(false);
    const formatDate = (date) => {
        if (!date) {
            return "";
        }
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }
    const getPointColor = (point) => {
        if (point < 1.5) return "#ff6666";
        if (point < 2) return "#ffa500";
        if (point < 2.5) return "#ffd700";
        if (point < 3.5) return "#9acd32";
        if (point < 4) return "#32cd32";
        return "#1e90ff";
    };
    return (
        open && <Modal open={open} onClose={handleClose}>
            <Box
                sx={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    transform: 'translate(-50%, -50%)',
                    width: "900px",
                    bgcolor: 'background.paper',
                    borderRadius: "16px",
                    boxShadow: "0 8px 24px rgba(0, 0, 0, 0.1)",
                    p: 4,
                    maxHeight: "80vh",
                    overflow: "auto"
                }}
            >
                <Typography
                    sx={{
                        textAlign: "center",
                        fontWeight: "bold",
                        mb: 3,
                        fontSize: "24px"
                    }}
                    variant="h5"
                >
                    Chi tiết sổ liên lạc
                </Typography>

                {selectedItem && (
                    <Box>
                        <Typography
                            variant="h6"
                            sx={{
                                fontWeight: "bold",
                                mb: 2,
                                fontSize: "20px"
                            }}
                        >
                            Đánh giá trước đó
                        </Typography>

                        <Typography mt={1} sx={{ color: "#5f6368" }}>
                            Thời gian: {formatDate(selectedItem?.from)} -{" "}
                            {formatDate(selectedItem?.to)}
                        </Typography>

                        <Stack direction="row" gap={2} mt={3}>
                            <DoneIcon sx={{ color: "green" }} />
                            <Typography fontWeight="bold" sx={{ color: "black" }}>Đã làm được</Typography>
                        </Stack>
                        <Typography sx={{ whiteSpace: "break-spaces" }}>
                            {selectedItem.achieved}
                        </Typography>

                        <Stack direction="row" gap={2} mt={3}>
                            <CloseIcon sx={{ color: "red" }} />
                            <Typography fontWeight="bold" sx={{ color: "black" }}>Chưa làm được</Typography>
                        </Stack>
                        <Typography sx={{ whiteSpace: "break-spaces", color: "black" }}>
                            {selectedItem.failed}
                        </Typography>

                        <Stack direction="row" gap={2} mt={3}>
                            <EditNoteIcon sx={{ color: "blue" }} />
                            <Typography fontWeight="bold" sx={{ color: "black" }}>Ghi chú thêm</Typography>
                        </Stack>
                        <Typography
                            sx={{
                                whiteSpace: "break-spaces",
                                fontStyle: selectedItem.noteFromTutor
                                    ? "normal"
                                    : "italic",
                                color: selectedItem.noteFromTutor ? "#333" : "#757575"
                            }}
                        >
                            {selectedItem.noteFromTutor || "Không có ghi chú"}
                        </Typography>

                        <Stack direction="row" gap={2} mt={3}>
                            <ListAltIcon sx={{ color: "#ff7043" }} />
                            <Typography fontWeight="bold" sx={{ color: "#5f6368" }}>Danh sách đánh giá</Typography>
                        </Stack>

                        <List
                            sx={{
                                height: "500px",
                                overflowY: "auto",
                                mt: 2,
                                borderRadius: "8px",
                                border: "1px solid #e0e0e0",
                                p: 1,
                                bgcolor: "#f9f9f9"
                            }}
                        >
                            {selectedItem.assessmentResults.map((a) => (
                                <ListItem
                                    key={a.id}
                                    sx={{
                                        mb: 1,
                                        borderBottom: "1px solid #e0e0e0",
                                        ":last-child": { borderBottom: "none" }
                                    }}
                                >
                                    <ListItemIcon>
                                        <ChevronRightIcon sx={{ color: "#7c4dff" }} />
                                    </ListItemIcon>
                                    <ListItemText
                                        primary={a.question}
                                        secondary={
                                            <Box>
                                                <Typography
                                                    sx={{
                                                        color: getPointColor(a.point),
                                                        fontWeight: "bold"
                                                    }}
                                                >
                                                    Điểm: {a.point}
                                                </Typography>
                                                <Typography
                                                    sx={{
                                                        color: "#757575",
                                                        fontStyle: "italic",
                                                        mt: 1
                                                    }}
                                                >
                                                    Chú thích: {a.selectedOptionText}
                                                </Typography>
                                            </Box>
                                        }
                                        primaryTypographyProps={{
                                            fontWeight: "bold",
                                            fontSize: "16px"
                                        }}
                                        secondaryTypographyProps={{
                                            sx: {
                                                color: getPointColor(a.point),
                                                fontSize: "14px"
                                            },
                                        }}
                                    />
                                </ListItem>
                            ))}
                        </List>
                    </Box>
                )}
            </Box>
        </Modal>


    )
}

export default ProgressReportDetail
