import { Avatar, Box, Button, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import LoadingComponent from '~/components/LoadingComponent';
import TablePagging from '~/components/TablePagging';
import TutorContext from '~/Context/TutorContext';
import services from '~/plugins/services';
import PAGES from '~/utils/pages';
import emptyForm from '~/assets/images/icon/emptyform.gif'
function TutorRegistrationTable({ status, searchValue, submit, startDate, endDate }) {
    const [loading, setLoading] = useState(false);
    const [listTutor, setListTutor] = useState([]);
    const [pagination, setPagination] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const nav = useNavigate();
    const formatDate = (date) => {
        const dateObj = new Date(date);
        const formattedDate = dateObj.getDate().toString().padStart(2, '0') + '/' +
            (dateObj.getMonth() + 1).toString().padStart(2, '0') + '/' +
            dateObj.getFullYear();
        return formattedDate;
    }
    useEffect(() => {
        handleGetTutor(1, 10);
    }, []);

    useEffect(() => {
        handleGetTutor(currentPage, status, searchValue, startDate, endDate)
    }, [currentPage, submit])

    const handleGetTutor = async (page, status, searchValue, startDate, endDate) => {
        try {
            setLoading(true);
            let submitStatus = ""
            if (status === 10)
                submitStatus = "Pending";
            if (status === 20)
                submitStatus = "Approve"
            if (status === 30)
                submitStatus = "Reject"
            await services.TutorManagementAPI.listTutor((res) => {
                res.pagination.currentSize = res.result.length
                setListTutor(res.result);
                setPagination(res.pagination)
            }, (err) => {
                console.log(err);
            }, {
                pageNumber: page,
                status: submitStatus,
                search: searchValue,
                startDate: startDate,
                endDate: endDate
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false)
        }
    }
    return (
        <TutorContext.Provider value={{ listTutor, setListTutor }}>
            {
                !listTutor || listTutor.length === 0 ? (
                    <Stack width="100%" alignItems="center" justifyContent="center">
                        <Box sx={{ textAlign: "center" }}>
                            <img src={emptyForm} style={{ height: "200px" }} />
                            <Typography mt={5} color="black">Không có dữ liệu!</Typography>
                        </Box>
                    </Stack>
                ) : (
                    <TableContainer component={Paper} sx={{ mt: "20px" }}>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>STT</TableCell>
                                    <TableCell>Người đăng ký</TableCell>
                                    <TableCell align='left'>Số điện thoại</TableCell>
                                    <TableCell align='center'>Ngày tạo</TableCell>
                                    <TableCell align='center'>Trạng thái đơn</TableCell>
                                    <TableCell>Hành động</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {
                                    listTutor.length !== 0 && listTutor?.map((tutor, index) => {
                                        return (
                                            <TableRow key={tutor.id}>
                                                <TableCell>{index + 1 + (currentPage - 1) * 10}</TableCell>
                                                <TableCell>
                                                    <Box sx={{ display: "flex", gap: 1 }}>
                                                        <Avatar alt={tutor.fullName} src={tutor.imageUrl} />
                                                        <Box>
                                                            <Typography sx={{ fontWeight: "bold" }}>{tutor.fullName}</Typography>
                                                            <Typography sx={{ fontSize: "12px" }}>{tutor.email}</Typography>
                                                        </Box>
                                                    </Box>
                                                </TableCell>
                                                <TableCell align='left'>
                                                    {tutor.phoneNumber}
                                                </TableCell>
                                                <TableCell align='center'>
                                                    {formatDate(tutor.createdDate)}
                                                </TableCell>
                                                <TableCell align='center'>
                                                    {
                                                        tutor.requestStatus === 0 && <Typography color="red" sx={{ fontSize: "12px" }}>Từ chối</Typography>
                                                    }
                                                    {
                                                        tutor.requestStatus === 1 && <Typography color="green" sx={{ fontSize: "12px" }}>Đã chấp nhận</Typography>
                                                    }
                                                    {
                                                        tutor.requestStatus === 2 && <Typography color="blue" sx={{ fontSize: "12px" }}>Đang chờ</Typography>
                                                    }
                                                </TableCell>
                                                <TableCell>
                                                    <Button onClick={() => { nav(PAGES.TUTORREGISTRATIONMANAGEMENT + "/" + tutor.id) }}>Xem chi tiết</Button>
                                                </TableCell>
                                            </TableRow>
                                        )
                                    })
                                }
                            </TableBody>
                        </Table>
                        <LoadingComponent open={loading} setOpen={setLoading} />
                        <TablePagging setCurrentPage={setCurrentPage} setPagination={setPagination} pagination={pagination} />
                    </TableContainer >
                )
            }
        </TutorContext.Provider >
    )
}

export default TutorRegistrationTable
